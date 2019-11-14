using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Initiator
{
    class BootstrapCommit : ProtocolCommit
    {
        public List<Account> BootstrapedAccounts { get; private set; }

        BootstrapCommit(ProtocolHandler protocol) : base(protocol) { }

        public async Task Init(RawBlock rawBlock)
        {
            var protocol = await Cache.GetProtocolAsync(rawBlock.Protocol);
            BootstrapedAccounts = new List<Account>(65);

            var stream = await Proto.Node.GetContractsAsync(level: 1);
            var contracts = await (Proto.Serializer as Serializer).DeserializeContracts(stream);
            var delegates = new List<Data.Models.Delegate>(8);

            #region bootstrap delegates
            foreach (var data in contracts.Where(x => x.Delegate == x.Address))
            {
                var baker = new Data.Models.Delegate
                {
                    Address = data.Address,
                    FirstLevel = rawBlock.Level,
                    LastLevel = rawBlock.Level,
                    ActivationLevel = 1,
                    DeactivationLevel = GracePeriod.Init(1, protocol.BlocksPerCycle, protocol.PreserverCycles),
                    Balance = data.Balance,
                    Counter = data.Counter,
                    PublicKey = data.Manager,
                    Staked = true,
                    Type = AccountType.Delegate
                };
                Cache.AddAccount(baker);
                BootstrapedAccounts.Add(baker);
                delegates.Add(baker);
            }
            #endregion

            #region bootstrap users
            foreach (var data in contracts.Where(x => x.Address[0] == 't' && String.IsNullOrEmpty(x.Delegate)))
            {
                var user = new User
                {
                    Address = data.Address,
                    FirstLevel = rawBlock.Level,
                    LastLevel = rawBlock.Level,
                    Balance = data.Balance,
                    Counter = data.Counter,
                    Type = AccountType.User,
                };
                Cache.AddAccount(user);
                BootstrapedAccounts.Add(user);
            }
            #endregion

            #region bootstrap contracts
            foreach (var data in contracts.Where(x => x.Address[0] == 'K'))
            {
                var contract = new Contract
                {
                    Address = data.Address,
                    FirstLevel = rawBlock.Level,
                    LastLevel = rawBlock.Level,
                    Balance = data.Balance,
                    Counter = data.Counter,
                    Spendable = false,
                    DelegationLevel = 1,
                    Delegate = await Cache.GetDelegateAsync(data.Delegate),
                    Manager = (User)await Cache.GetAccountAsync(data.Manager),
                    Staked = !String.IsNullOrEmpty(data.Delegate),
                    Type = AccountType.Contract,
                    Kind = ContractKind.SmartContract,
                };

                Cache.AddAccount(contract);
                BootstrapedAccounts.Add(contract);
            }
            #endregion

            #region stats
            foreach (var baker in delegates)
            {
                var delegators = BootstrapedAccounts.Where(x => x.Delegate == baker);

                baker.Delegators = delegators.Count();
                baker.StakingBalance = baker.Balance
                    + (baker.Delegators > 0 ? delegators.Sum(x => x.Balance) : 0);
            }
            #endregion
        }

        public async Task Init(Block block)
        {
            BootstrapedAccounts = await Db.Accounts.Where(x => x.Counter == 0).ToListAsync();
        }

        public override Task Apply()
        {
            Db.Accounts.AddRange(BootstrapedAccounts);

            return Task.CompletedTask;
        }

        public override Task Revert()
        {
            Db.Accounts.RemoveRange(BootstrapedAccounts);
            Cache.RemoveAccounts(BootstrapedAccounts);

            return Task.CompletedTask;
        }

        #region static
        public static async Task<BootstrapCommit> Apply(ProtocolHandler proto, RawBlock rawBlock)
        {
            var commit = new BootstrapCommit(proto);
            await commit.Init(rawBlock);
            await commit.Apply();

            return commit;
        }

        public static async Task<BootstrapCommit> Revert(ProtocolHandler proto, Block block)
        {
            var commit = new BootstrapCommit(proto);
            await commit.Init(block);
            await commit.Revert();

            return commit;
        }
        #endregion
    }
}
