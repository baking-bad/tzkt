using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;

using Tezzycat.Data;
using Tezzycat.Data.Models;

namespace Tezzycat.Sync.Services.Protocols
{
    public class InitializationHandler : GenesisHandler
    {
        protected readonly TezosNode Node;
        protected readonly Dictionary<string, Contract> Contracts;

        public InitializationHandler(TezosNode node, SyncContext db, IMemoryCache cache)
            :base(db, cache)
        {
            Node = node;
            Contracts = new Dictionary<string, Contract>(64);
        }

        #region IProtocolHandler
        public override string Kind => "Initialization";

        public override async Task<AppState> ApplyBlock(JObject json)
        {
            var block = await ParseBlock(json);

            if (block.Level != 1)
                throw new Exception("Initialization block must be at level 1");

            if (block.Protocol.Blocks > 0)
                throw new Exception("Initialization block already exists");

            await SeedContracts();

            await InitCycle(0);
            await InitCycle(1);
            await InitCycle(2);
            await InitCycle(3);
            await InitCycle(4);
            await InitCycle(5);
            await InitCycle(6);

            Db.Blocks.Add(block);
            ProtocolUp(block.Protocol);
            await SetAppState(block);

            await Db.SaveChangesAsync();
            return await GetAppStateAsync();
        }

        public override async Task<AppState> RevertLastBlock()
        {
            var lastBlock = await GetLastBlock();

            if (lastBlock == null)
                throw new Exception("Nothing to revert");

            if (lastBlock.Level != 1)
                throw new Exception("Initialization block must be at level 1");

            await ClearCycle(6);
            await ClearCycle(5);
            await ClearCycle(4);
            await ClearCycle(3);
            await ClearCycle(2);
            await ClearCycle(1);
            await ClearCycle(0);

            await ClearContracts();

            Db.Blocks.Remove(lastBlock);
            ProtocolDown(lastBlock.Protocol);
            await SetAppState(await GetSecondLastBlock());

            await Db.SaveChangesAsync();
            return await GetAppStateAsync();
        }
        #endregion

        #region virtual
        protected virtual Contract GetContract(string address)
        {
            if (!Contracts.TryGetValue(address, out var contract))
            {
                if (!address.StartsWith("tz"))
                    throw new Exception($"Contract {address} doesn't exist");

                contract = new Contract
                {
                    Kind = ContractKind.Account,
                    Address = address,
                    Spendable = true
                };

                Db.Contracts.Add(contract);
                Contracts[address] = contract;
            }
            return contract;
        }

        protected virtual async Task<Block> GetSecondLastBlock()
        {
            var state = await GetAppStateAsync();

            return await Db.Blocks
                .Include(x => x.Protocol)
                .FirstOrDefaultAsync(x => x.Level == state.Level - 1);
        }
        #endregion

        private async Task SeedContracts()
        {
            Contracts.Clear();
            var contracts = await Node.GetContractsAsync(1);
            #region seed
            foreach (var data in contracts)
            {
                var address = data[0].String();
                var contract = data[1];

                Contracts[address] = new Contract
                {
                    Kind = address.StartsWith("tz")
                            ? contract["delegate"] == null
                                ? ContractKind.Account
                                : ContractKind.Baker
                            : contract["code"] == null
                                ? ContractKind.Originated
                                : ContractKind.SmartContract,
                    Address = address,

                    Delegatable = contract["delegatable"]?.Bool() ?? false,
                    Spendable = contract["spendable"]?.Bool() ?? false,
                    Staked = contract["delegate"] != null,
                    
                    Balance = contract["balance"].Int64(),
                    Counter = contract["counter"].Int64(),
                    Frozen = 0
                };
            }
            #endregion
            #region relations
            foreach (var data in contracts)
            {
                var address = data[0].String();
                var contract = data[1];
                var delegatee = contract["delegate"]?.String();
                var manager = contract["manager"]?.String();

                if (address.StartsWith("KT"))
                {
                    if (delegatee != null)
                        Contracts[address].Delegate = GetContract(delegatee);

                    if (manager?.StartsWith("tz") != true)
                        throw new Exception("This should never happen. KT should have a manager");

                    Contracts[address].Manager = GetContract(manager);
                }
                else
                {
                    if (delegatee != null && delegatee != address)
                        throw new Exception("This should never happen. Tz can't delegate to another tz");

                    if (manager?.StartsWith("tz") == false)
                        Contracts[address].PublicKey = manager;
                }
            }
            #endregion
            #region stats
            foreach (var contract in Contracts.Values.Where(x => x.Kind == ContractKind.Baker))
            {
                var delegators = Contracts.Values.Where(x => x.Delegate == contract);

                contract.DelegatorsCount = delegators.Count();
                contract.StakingBalance = contract.Balance
                    + (contract.DelegatorsCount > 0 ? delegators.Sum(x => x.Balance) : 0);
            }
            #endregion
            Db.Contracts.AddRange(Contracts.Values);
        }
        private async Task ClearContracts()
        {
            var contracts = await Db.Contracts.ToListAsync();
            Db.Contracts.RemoveRange(contracts);
            Contracts.Clear();
        }

        private async Task InitCycle(int cycle)
        {
            #region init rights
            var rights = await Task.WhenAll(
                Node.GetBakingRightsAsync(cycle, 4096, 1),
                Node.GetEndorsingRightsAsync(cycle, 4096));

            var bakingRights = rights[0]
                .Select(x => new BakingRight
                {
                    Baker = GetContract(x["delegate"].String()),
                    Level = x["level"].Int32(),
                    Priority = x["priority"].Int32()
                });

            var endorsingRights = rights[1]
                .Select(x => new EndorsingRight
                {
                    Baker = GetContract(x["delegate"].String()),
                    Level = x["level"].Int32(),
                    Slots = x["slots"].Count()
                });

            //Db.BakingRights.AddRange(bakingRights);
            //Db.EndorsingRights.AddRange(endorsingRights);
            #endregion

            #region init cycle
            var cycleStat = new CycleStat
            {
                Cycle = cycle,
                Snapshot = 1,
            };
            Db.CycleStats.Add(cycleStat);
            #endregion

            #region init snapshots
            var snapshots = Contracts.Values
                .Where(x => x.Staked)
                .Select(x => new BalanceSnapshot
                {
                    Balance = x.Balance,
                    Contract = x,
                    Delegate = GetContract(x.Delegate?.Address ?? x.Address),
                    Level = cycleStat.Snapshot
                });
            #endregion

            #region init delegators
            var delegators = snapshots
                .Where(x => x.Contract.Kind != ContractKind.Baker)
                .Select(x => new DelegatorStat
                {
                    Baker = x.Delegate,
                    Balance = x.Balance,
                    Delegator = x.Contract,
                    Cycle = cycle
                });
            Db.DelegatorStats.AddRange(delegators);
            #endregion

            #region init bakers
            var bakers = snapshots
                .Where(x => x.Contract.Kind == ContractKind.Baker)
                .Select(x => new BakerStat
                {
                    Baker = x.Contract,
                    Balance = x.Balance,
                    Cycle = cycle,
                    StakingBalance = snapshots
                        .Where(s => s.Delegate == x.Contract)
                        .Sum(s => s.Balance),
                    Blocks = bakingRights
                        .Count(r => r.Priority == 0 && r.Baker == x.Contract),
                    Endorsements = endorsingRights
                        .Where(r => r.Baker == x.Contract)
                        .DefaultIfEmpty(new EndorsingRight())
                        .Sum(r => r.Slots)
                });
            Db.BakerStats.AddRange(bakers);
            #endregion
        }
        private async Task ClearCycle(int cycle)
        {
            //Db.BakingRights.RemoveRange(
            //    await Db.BakingRights.Where(x => (x.Level - 1) / 4096 == cycle).ToListAsync());

            //Db.EndorsingRights.RemoveRange(
            //    await Db.EndorsingRights.Where(x => (x.Level - 1) / 4096 == cycle).ToListAsync());

            Db.CycleStats.Remove(
                await Db.CycleStats.FirstAsync(x => x.Cycle == cycle));

            Db.DelegatorStats.RemoveRange(
                await Db.DelegatorStats.Where(x => x.Cycle == cycle).ToListAsync());

            Db.BakerStats.RemoveRange(
                await Db.BakerStats.Where(x => x.Cycle == cycle).ToListAsync());
        }
    }
}
