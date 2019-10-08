using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Tzkt.Data.Models;
using Tzkt.Sync.Services;

namespace Tzkt.Sync.Protocols.Initiator
{
    class BootstrapCommit : ProtocolCommit
    {
        public List<Account> BootstrapedAccounts { get; private set; }

        readonly TezosNode Node;
        readonly Serializer Serializer;

        public BootstrapCommit(InitiatorHandler protocol, List<ICommit> commits) : base(protocol, commits)
        {
            Node = protocol.Node;
            Serializer = protocol.Serializer as Serializer;
        }

        public override Task Init(IBlock block)
        {
            return Task.CompletedTask;
        }

        public override async Task Apply()
        {
            var stream = await Node.GetContractsAsync(level: 1);
            var contracts = await Serializer.DeserializeContracts(stream);
            var delegates = new List<Data.Models.Delegate>(8);
            var accounts = new List<Account>(64);

            #region seed delegates
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
                Accounts.AddAccount(baker);
                delegates.Add(baker);
                accounts.Add(baker);
            }
            #endregion

            #region seed users
            foreach (var data in contracts.Where(x => x.Address[0] == 't' && String.IsNullOrEmpty(x.Delegate)))
            {
                var user = new User
                {
                    Address = data.Address,
                    Balance = data.Balance,
                    Counter = data.Counter,
                    Type = AccountType.User,
                };
                Accounts.AddAccount(user);
                accounts.Add(user);
            }
            #endregion

            #region seed contracts
            foreach (var data in contracts.Where(x => x.Address[0] == 'K'))
            {
                var contract = new Contract
                {
                    Address = data.Address,
                    Balance = data.Balance,
                    Counter = data.Counter,
                    DelegationLevel = 1,
                    Manager = (User)await Accounts.GetAccountAsync(data.Manager),
                    Staked = !String.IsNullOrEmpty(data.Delegate),
                    Type = AccountType.Contract,
                };

                if (!String.IsNullOrEmpty(data.Delegate))
                    contract.Delegate = (Data.Models.Delegate)await Accounts.GetAccountAsync(data.Delegate);

                Accounts.AddAccount(contract);
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

        public override Task Revert()
        {
            if (BootstrapedAccounts == null)
                throw new Exception("Commit is not initialized");

            Db.Accounts.RemoveRange(BootstrapedAccounts);
            Accounts.Clear(true);

            return Task.CompletedTask;
        }

        #region static
        public static async Task<BootstrapCommit> Create(InitiatorHandler protocol, List<ICommit> commits, RawBlock rawBlock)
        {
            var commit = new BootstrapCommit(protocol, commits);
            await commit.Init(rawBlock);
            return commit;
        }

        public static Task<BootstrapCommit> Create(InitiatorHandler protocol, List<ICommit> commits, List<Account> accounts)
        {
            var commit = new BootstrapCommit(protocol, commits) { BootstrapedAccounts = accounts };
            return Task.FromResult(commit);
        }
        #endregion
    }
}
