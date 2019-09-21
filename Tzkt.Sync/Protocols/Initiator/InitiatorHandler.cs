using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

using Tzkt.Data;
using Tzkt.Data.Models;
using Tzkt.Sync.Services;

namespace Tzkt.Sync.Protocols
{
    public class InitiatorHandler : GenesisHandler
    {
        public override string Protocol => "Initiator";

        protected readonly TezosNode Node;
        protected readonly AccountsCache AccountsCache;

        public InitiatorHandler(TezosNode node, TzktContext db, AccountsCache accountsCache, ProtocolsCache protoCache, StateCache stateCache)
            : base(db, protoCache, stateCache)
        {
            Node = node;
            AccountsCache = accountsCache;
        }

        public override async Task<AppState> ApplyBlock(JObject json)
        {
            var block = await ParseBlock(json);

            if (block.Level != 1)
                throw new Exception("Initialization block must be at level 1");

            if (block.Protocol.Weight > 0)
                throw new Exception("Initialization block already exists");

            await StartVotingEpoch();
            await SeedAccountsAsync();

            Db.Blocks.Add(block);
            ProtoCache.ProtocolUp(block.Protocol);
            await StateCache.SetAppStateAsync(block);

            await Db.SaveChangesAsync();
            return await StateCache.GetAppStateAsync();
        }

        public override async Task<AppState> RevertLastBlock()
        {
            var currentBlock = await StateCache.GetCurrentBlock();

            if (currentBlock == null)
                throw new Exception("Nothing to revert");

            if (currentBlock.Level != 1)
                throw new Exception("Initialization block must be at level 1");

            await ClearAccounts();
            await RevertVotingEpoch();

            Db.Blocks.Remove(currentBlock);
            ProtoCache.ProtocolDown(currentBlock.Protocol);
            await StateCache.SetAppStateAsync(await StateCache.GetPreviousBlock());

            await Db.SaveChangesAsync();
            return await StateCache.GetAppStateAsync();
        }

        protected virtual Task StartVotingEpoch()
        {
            var epoch = new VotingEpoch { Level = 1 };
            var period = new ProposalPeriod
            {
                Epoch = epoch,
                Kind = VotingPeriods.Proposal,
                StartLevel = 1,
                EndLevel = 32768
            };

            Db.VotingEpoches.Add(epoch);
            Db.VotingPeriods.Add(period);
            return Task.CompletedTask;
        }
        protected virtual async Task RevertVotingEpoch()
        {
            var epoches = await Db.VotingEpoches
                .Include(x => x.Periods)
                .ToListAsync();

            Db.VotingEpoches.RemoveRange(epoches);
        }

        private async Task SeedAccountsAsync()
        {
            var contracts = await Node.GetContractsAsync(level: 1);
            var delegates = new List<Data.Models.Delegate>(8);
            var accounts = new List<Account>(64);

            #region seed delegates
            foreach (var data in contracts.Where(x => x[1]["delegate"]?.String() == x[0].String()))
            {
                var baker = new Data.Models.Delegate
                {
                    Address = data[0].String(),
                    ActivationLevel = 1,
                    Balance = data[1]["balance"].Int64(),
                    Counter = data[1]["counter"].Int64(),
                    PublicKey = data[1]["manager"].String(),
                    Staked = true,
                    Type = AccountType.Delegate
                };
                AccountsCache.AddAccount(baker);
                delegates.Add(baker);
                accounts.Add(baker);
            }
            #endregion

            #region seed users
            foreach (var data in contracts.Where(x => x[0].String()[0] == 't' && x[1]["delegate"] == null))
            {
                var user = new User
                {
                    Address = data[0].String(),
                    Balance = data[1]["balance"].Int64(),
                    Counter = data[1]["counter"].Int64(),
                    Type = AccountType.User, 
                };
                AccountsCache.AddAccount(user);
                accounts.Add(user);
            }
            #endregion

            #region seed contracts
            foreach (var data in contracts.Where(x => x[0].String()[0] == 'K'))
            {
                var contract = new Contract
                {
                    Address = data[0].String(),
                    Balance = data[1]["balance"].Int64(),
                    Counter = data[1]["counter"].Int64(),
                    DelegationLevel = 1,
                    Manager = (User)await AccountsCache.GetAccountAsync(data[1]["manager"].String()),
                    Staked = data[1]["delegate"] != null,
                    Type = AccountType.Contract,
                };

                if (data[1]["delegate"] != null)
                    contract.Delegate = (Data.Models.Delegate)await AccountsCache.GetAccountAsync(data[1]["delegate"].String());

                AccountsCache.AddAccount(contract);
                accounts.Add(contract);
            }
            #endregion

            #region stats
            foreach (var baker in delegates)
            {
                var delegators = accounts.Where(x => x.Delegate == baker);

                baker.Delegators = delegators.Count();
                baker.StakingBalance = baker.Balance
                    + (baker.Delegators > 0 ? delegators.Sum(x => x.Balance) : 0);
            }
            #endregion
        }
        private async Task ClearAccounts()
        {
            var accounts = await Db.Accounts.ToListAsync();
            Db.Accounts.RemoveRange(accounts);
            AccountsCache.Clear(true);
        }

        /*private async Task InitCycle(int cycle)
        {
            #region init rights
            var rights = await Task.WhenAll(
                Node.GetBakingRightsAsync(1, cycle, 1),
                Node.GetEndorsingRightsAsync(1, cycle));

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

            Db.BakingRights.AddRange(bakingRights);
            Db.EndorsingRights.AddRange(endorsingRights);
            #endregion

            #region init cycle
            var cycleObj = new Cycle
            {
                Index = cycle,
                Snapshot = 1,
            };
            Db.Cycles.Add(cycleObj);
            #endregion

            #region init snapshots
            var snapshots = Contracts.Values
                .Where(x => x.Staked)
                .Select(x => new BalanceSnapshot
                {
                    Balance = x.Balance,
                    Address = x,
                    Delegate = GetContract(x.Delegate?.Address ?? x.Address),
                    Level = cycleObj.Snapshot
                });
            #endregion

            #region init delegators
            var delegators = snapshots
                .Where(x => x.Contract.Kind != ContractKind.Baker)
                .Select(x => new DelegatorSnapshot
                {
                    Baker = x.Delegate,
                    Balance = x.Balance,
                    Delegator = x.Contract,
                    Cycle = cycle
                });
            Db.DelegatorSnapshots.AddRange(delegators);
            #endregion

            #region init bakers
            var bakers = snapshots
                .Where(x => x.Contract.Kind == ContractKind.Baker)
                .Select(x => new BakingCycle
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
            Db.BakerCycles.AddRange(bakers);
            #endregion
        }*/
        /*private async Task ClearCycle(int cycle)
        {
            Db.BakingRights.RemoveRange(
                await Db.BakingRights.Where(x => (x.Level - 1) / 4096 == cycle).ToListAsync());

            Db.EndorsingRights.RemoveRange(
                await Db.EndorsingRights.Where(x => (x.Level - 1) / 4096 == cycle).ToListAsync());

            Db.Cycles.Remove(
                await Db.Cycles.FirstAsync(x => x.Index == cycle));

            Db.DelegatorSnapshots.RemoveRange(
                await Db.DelegatorSnapshots.Where(x => x.Cycle == cycle).ToListAsync());

            Db.BakerCycles.RemoveRange(
                await Db.BakerCycles.Where(x => x.Cycle == cycle).ToListAsync());
        }*/
    }
}
