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

        public BootstrapCommit(InitiatorHandler protocol, List<ICommit> commits) : base(protocol, commits) { }

        public override async Task Init()
        {
            BootstrapedAccounts = await Db.Accounts.ToListAsync();
        }

        public override async Task Init(IBlock block)
        {
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
                    ActivationLevel = 1,
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
                    Balance = data.Balance,
                    Counter = data.Counter,
                    DelegationLevel = 1,
                    Manager = (User)await Cache.GetAccountAsync(data.Manager),
                    Staked = !String.IsNullOrEmpty(data.Delegate),
                    Type = AccountType.Contract,
                };

                if (!String.IsNullOrEmpty(data.Delegate))
                    contract.Delegate = (Data.Models.Delegate)await Cache.GetAccountAsync(data.Delegate);

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

        public override Task Apply()
        {
            if (BootstrapedAccounts == null)
                throw new Exception("Commit is not initialized");

            Db.Accounts.AddRange(BootstrapedAccounts);

            return Task.CompletedTask;
        }

        public override Task Revert()
        {
            if (BootstrapedAccounts == null)
                throw new Exception("Commit is not initialized");

            Db.Accounts.RemoveRange(BootstrapedAccounts);
            Cache.RemoveAccounts(BootstrapedAccounts);

            return Task.CompletedTask;
        }

        #region static
        public static async Task<BootstrapCommit> Create(InitiatorHandler protocol, List<ICommit> commits, RawBlock rawBlock)
        {
            var commit = new BootstrapCommit(protocol, commits);
            await commit.Init(rawBlock);
            return commit;
        }

        public static async Task<BootstrapCommit> Create(InitiatorHandler protocol, List<ICommit> commits)
        {
            var commit = new BootstrapCommit(protocol, commits);
            await commit.Init();
            return commit;
        }
        #endregion
    }
}
