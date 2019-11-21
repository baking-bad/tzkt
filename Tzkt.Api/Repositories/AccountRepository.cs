using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Dapper;

using Tzkt.Api.Models;
using Tzkt.Api.Services;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Repositories
{
    public class AccountRepository : DbConnection
    {
        readonly AccountsCache Accounts;
        readonly AliasService Aliases;
        readonly StateCache State;
        readonly OperationRepository Operations;

        public AccountRepository(AccountsCache accounts, AliasService aliases, StateCache state, OperationRepository operations, IConfiguration config) : base(config)
        {
            Accounts = accounts;
            Aliases = aliases;
            State = state;
            Operations = operations;
        }

        public async Task<IAccount> Get(string address)
        {
            var rawAccount = await Accounts.Get(address);

            if (rawAccount == null)
                return address[0] == 't'
                    ? new User
                    {
                        Address = address,
                        Counter = State.GetCounter(),
                    }
                    : null;

            switch (rawAccount)
            {
                case RawDelegate delegat:
                    var deactivation = (int?)delegat.DeactivationLevel;
                    var active = deactivation > State.GetLevel();
                    return new Models.Delegate
                    {
                        Active = active,
                        Alias = Aliases[delegat.Id].Name,
                        Address = address,
                        PublicKey = delegat.PublicKey,
                        Balance = delegat.Balance,
                        FrozenDeposits = delegat.FrozenDeposits,
                        FrozenRewards = delegat.FrozenRewards,
                        FrozenFees = delegat.FrozenFees,
                        Counter = delegat.Counter,
                        ActivationLevel = delegat.ActivationLevel,
                        DeactivationLevel = active ? null : deactivation,
                        DelegatorsCount = delegat.Delegators,
                        StakingBalance = delegat.StakingBalance,
                        FirstActivity = delegat.FirstLevel,
                        LastActivity = delegat.LastLevel
                    };
                case RawUser user:
                    return new User
                    {
                        Alias = Aliases[user.Id].Name,
                        Address = address,
                        Balance = user.Balance,
                        Counter = user.Balance > 0 ? user.Counter : State.GetCounter(),
                        FirstActivity = user.FirstLevel,
                        LastActivity = user.LastLevel,
                        PublicKey = user.PublicKey,
                        Delegate = user.DelegateId != null ? new DelegateInfo(Aliases[(int)user.DelegateId], user.Staked) : null
                    };
                case RawContract contract:
                    var managerAlias = Aliases[contract.ManagerId];
                    var manager = (RawUser)await Accounts.Get(managerAlias.Address);
                    return new Contract
                    {
                        Kind = KindToString(contract.Kind),
                        Alias = Aliases[contract.Id].Name,
                        Address = address,
                        Balance = contract.Balance,
                        Delegate = contract.DelegateId != null ? new DelegateInfo(Aliases[(int)contract.DelegateId], contract.Staked) : null,
                        Manager = new ManagerInfo(managerAlias, manager.PublicKey),
                        FirstActivity = contract.FirstLevel,
                        LastActivity = contract.LastLevel
                    };
                default:
                    throw new Exception($"Invalid raw account type");
            }
        }

        public async Task<IAccount> GetProfile(string address)
        {
            var account = await Get(address);

            switch (account)
            {
                case Models.Delegate delegat:
                    delegat.Delegators = await GetDelegators(address, 20);
                    delegat.Operations = await GetOperations(address,
                        Data.Models.Operations.All &
                        ~Data.Models.Operations.Endorsements &
                        ~Data.Models.Operations.Revelations, 20);
                    break;
                case User user when user.FirstActivity != null:
                    user.Operations = await GetOperations(address,
                        Data.Models.Operations.Manager |
                        Data.Models.Operations.Activations, 20);
                    break;
                case Contract contract:
                    contract.Operations = await GetOperations(address,
                        Data.Models.Operations.Manager, 20);
                    break;
            }

            return account;
        }

        public async Task<IEnumerable<IAccount>> Get(int limit = 100, int offset = 0)
        {
            var sql = @"
                SELECT      account.*, manager.""PublicKey"" as ""ManagerPublicKey""
                FROM        ""Accounts"" as account
                LEFT JOIN   ""Accounts"" as manager ON manager.""Id"" = account.""ManagerId""
                ORDER BY    account.""Id""
                OFFSET      @offset
                LIMIT       @limit";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { limit, offset });

            var accounts = new List<IAccount>(rows.Count());
            foreach (var row in rows)
            {
                switch ((int)row.Type)
                {
                    case 0:
                        accounts.Add(new User
                        {
                            Alias = Aliases[row.Id].Name,
                            Address = row.Address,
                            Balance = row.Balance,
                            Counter = row.Balance > 0 ? row.Counter : State.GetCounter(),
                            FirstActivity = row.FirstLevel,
                            LastActivity = row.LastLevel,
                            PublicKey = row.PublicKey,
                            Delegate = row.DelegateId != null ? new DelegateInfo(Aliases[row.DelegateId], row.Staked) : null
                        });
                        break;
                    case 1:
                        var deactivation = (int?)row.DeactivationLevel;
                        var active = deactivation > State.GetLevel();
                        accounts.Add(new Models.Delegate
                        {
                            Active = active,
                            Alias = Aliases[row.Id].Name,
                            Address = row.Address,
                            PublicKey = row.PublicKey,
                            Balance = row.Balance,
                            FrozenDeposits = row.FrozenDeposits,
                            FrozenRewards = row.FrozenRewards,
                            FrozenFees = row.FrozenFees,
                            Counter = row.Counter,
                            ActivationLevel = row.ActivationLevel,
                            DeactivationLevel = active ? null : deactivation,
                            DelegatorsCount = row.Delegators,
                            StakingBalance = row.StakingBalance,
                            FirstActivity = row.FirstLevel,
                            LastActivity = row.LastLevel
                        });
                        break;
                    case 2:
                        accounts.Add(new Contract
                        {
                            Kind = KindToString(row.Kind),
                            Alias = Aliases[row.Id].Name,
                            Address = row.Address,
                            Balance = row.Balance,
                            Delegate = row.DelegateId != null ? new DelegateInfo(Aliases[row.DelegateId], row.Staked) : null,
                            Manager = new ManagerInfo(Aliases[row.ManagerId], row.ManagerPublicKey),
                            FirstActivity = row.FirstLevel,
                            LastActivity = row.LastLevel
                        });
                        break;
                }
            }

            return accounts;
        }

        public async Task<IEnumerable<DelegatorInfo>> GetDelegators(string address, int limit = 100, int offset = 0)
        {
            var delegat = await Accounts.Get(address);

            var sql = @"
                SELECT      ""Id"", ""Balance"", ""DelegationLevel""
                FROM        ""Accounts""
                WHERE       ""DelegateId"" = @delegateId
                ORDER BY    ""DelegationLevel"" DESC
                OFFSET      @offset
                LIMIT       @limit";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { delegateId = delegat.Id, limit, offset });

            return rows.Select(row => new DelegatorInfo
            {
                Alias = Aliases[row.Id].Name, 
                Address = Aliases[row.Id].Address,
                Balance = row.Balance,
                DelegationLevel = row.DelegationLevel
            });
        }

        public async Task<IEnumerable<IOperation>> GetOperations(string address, Data.Models.Operations operations, int limit = 100)
        {
            var account = await Accounts.Get(address);
            var resultOps = account.Operations & operations;
            var result = new List<IOperation>(limit * 2);

            switch (account)
            {
                case RawDelegate delegat:

                    var endorsements = resultOps.HasFlag(Data.Models.Operations.Endorsements)
                        ? Operations.GetLastEndorsements(account, limit)
                        : Task.FromResult(Enumerable.Empty<EndorsementOperation>());

                    var proposals = resultOps.HasFlag(Data.Models.Operations.Proposals)
                        ? Operations.GetLastProposals(account, limit)
                        : Task.FromResult(Enumerable.Empty<ProposalOperation>());

                    var ballots = resultOps.HasFlag(Data.Models.Operations.Ballots)
                        ? Operations.GetLastBallots(account, limit)
                        : Task.FromResult(Enumerable.Empty<BallotOperation>());

                    var activations = resultOps.HasFlag(Data.Models.Operations.Activations)
                        ? Operations.GetLastActivations(account, limit)
                        : Task.FromResult(Enumerable.Empty<ActivationOperation>());

                    var doubleBaking = resultOps.HasFlag(Data.Models.Operations.DoubleBakings)
                        ? Operations.GetLastDoubleBakings(account, limit)
                        : Task.FromResult(Enumerable.Empty<DoubleBakingOperation>());

                    var doubleEndorsing = resultOps.HasFlag(Data.Models.Operations.DoubleEndorsings)
                        ? Operations.GetLastDoubleEndorsings(account, limit)
                        : Task.FromResult(Enumerable.Empty<DoubleEndorsingOperation>());

                    var nonceRevelations = resultOps.HasFlag(Data.Models.Operations.Revelations)
                        ? Operations.GetLastNonceRevelations(account, limit)
                        : Task.FromResult(Enumerable.Empty<NonceRevelationOperation>());

                    var delegations = resultOps.HasFlag(Data.Models.Operations.Delegations)
                        ? Operations.GetLastDelegations(account, limit)
                        : Task.FromResult(Enumerable.Empty<DelegationOperation>());

                    var originations = resultOps.HasFlag(Data.Models.Operations.Originations)
                        ? Operations.GetLastOriginations(account, limit)
                        : Task.FromResult(Enumerable.Empty<OriginationOperation>());

                    var transactions = resultOps.HasFlag(Data.Models.Operations.Transactions)
                        ? Operations.GetLastTransactions(account, limit)
                        : Task.FromResult(Enumerable.Empty<TransactionOperation>());

                    var reveals = resultOps.HasFlag(Data.Models.Operations.Reveals)
                        ? Operations.GetLastReveals(account, limit)
                        : Task.FromResult(Enumerable.Empty<RevealOperation>());

                    await Task.WhenAll(
                        endorsements,
                        proposals,
                        ballots,
                        activations,
                        doubleBaking,
                        doubleEndorsing,
                        nonceRevelations,
                        delegations,
                        originations,
                        transactions,
                        reveals);

                    result.AddRange(endorsements.Result);
                    result.AddRange(proposals.Result);
                    result.AddRange(ballots.Result);
                    result.AddRange(activations.Result);
                    result.AddRange(doubleBaking.Result);
                    result.AddRange(doubleEndorsing.Result);
                    result.AddRange(nonceRevelations.Result);
                    result.AddRange(delegations.Result);
                    result.AddRange(originations.Result);
                    result.AddRange(transactions.Result);
                    result.AddRange(reveals.Result);

                    break;
                case RawUser user:

                    var userActivations = resultOps.HasFlag(Data.Models.Operations.Activations)
                        ? Operations.GetLastActivations(account, limit)
                        : Task.FromResult(Enumerable.Empty<ActivationOperation>());

                    var userDelegations = resultOps.HasFlag(Data.Models.Operations.Delegations)
                        ? Operations.GetLastDelegations(account, limit)
                        : Task.FromResult(Enumerable.Empty<DelegationOperation>());

                    var userOriginations = resultOps.HasFlag(Data.Models.Operations.Originations)
                        ? Operations.GetLastOriginations(account, limit)
                        : Task.FromResult(Enumerable.Empty<OriginationOperation>());

                    var userTransactions = resultOps.HasFlag(Data.Models.Operations.Transactions)
                        ? Operations.GetLastTransactions(account, limit)
                        : Task.FromResult(Enumerable.Empty<TransactionOperation>());

                    var userReveals = resultOps.HasFlag(Data.Models.Operations.Reveals)
                        ? Operations.GetLastReveals(account, limit)
                        : Task.FromResult(Enumerable.Empty<RevealOperation>());

                    await Task.WhenAll(
                        userActivations,
                        userDelegations,
                        userOriginations,
                        userTransactions,
                        userReveals);

                    result.AddRange(userActivations.Result);
                    result.AddRange(userDelegations.Result);
                    result.AddRange(userOriginations.Result);
                    result.AddRange(userTransactions.Result);
                    result.AddRange(userReveals.Result);

                    break;
                case RawContract contract:

                    var contractDelegations = resultOps.HasFlag(Data.Models.Operations.Delegations)
                        ? Operations.GetLastDelegations(account, limit)
                        : Task.FromResult(Enumerable.Empty<DelegationOperation>());

                    var contractOriginations = resultOps.HasFlag(Data.Models.Operations.Originations)
                        ? Operations.GetLastOriginations(account, limit)
                        : Task.FromResult(Enumerable.Empty<OriginationOperation>());

                    var contractTransactions = resultOps.HasFlag(Data.Models.Operations.Transactions)
                        ? Operations.GetLastTransactions(account, limit)
                        : Task.FromResult(Enumerable.Empty<TransactionOperation>());

                    var contractReveals = resultOps.HasFlag(Data.Models.Operations.Reveals)
                        ? Operations.GetLastReveals(account, limit)
                        : Task.FromResult(Enumerable.Empty<RevealOperation>());

                    await Task.WhenAll(
                        contractDelegations,
                        contractOriginations,
                        contractTransactions,
                        contractReveals);

                    result.AddRange(contractDelegations.Result);
                    result.AddRange(contractOriginations.Result);
                    result.AddRange(contractTransactions.Result);
                    result.AddRange(contractReveals.Result);

                    break;
            }

            return result.OrderByDescending(x => x.Id).Take(limit);
        }

        public async Task<IEnumerable<IOperation>> GetOperations(string address, Data.Models.Operations operations, int fromLevel, int limit = 100)
        {
            var account = await Accounts.Get(address);
            var resultOps = account.Operations & operations;
            var result = new List<IOperation>(limit * 2);

            switch (account)
            {
                case RawDelegate delegat:

                    var endorsements = resultOps.HasFlag(Data.Models.Operations.Endorsements)
                        ? Operations.GetLastEndorsements(account, fromLevel, limit)
                        : Task.FromResult(Enumerable.Empty<EndorsementOperation>());

                    var proposals = resultOps.HasFlag(Data.Models.Operations.Proposals)
                        ? Operations.GetLastProposals(account, fromLevel, limit)
                        : Task.FromResult(Enumerable.Empty<ProposalOperation>());

                    var ballots = resultOps.HasFlag(Data.Models.Operations.Ballots)
                        ? Operations.GetLastBallots(account, fromLevel, limit)
                        : Task.FromResult(Enumerable.Empty<BallotOperation>());

                    var activations = resultOps.HasFlag(Data.Models.Operations.Activations)
                        ? Operations.GetLastActivations(account, fromLevel, limit)
                        : Task.FromResult(Enumerable.Empty<ActivationOperation>());

                    var doubleBaking = resultOps.HasFlag(Data.Models.Operations.DoubleBakings)
                        ? Operations.GetLastDoubleBakings(account, fromLevel, limit)
                        : Task.FromResult(Enumerable.Empty<DoubleBakingOperation>());

                    var doubleEndorsing = resultOps.HasFlag(Data.Models.Operations.DoubleEndorsings)
                        ? Operations.GetLastDoubleEndorsings(account, fromLevel, limit)
                        : Task.FromResult(Enumerable.Empty<DoubleEndorsingOperation>());

                    var nonceRevelations = resultOps.HasFlag(Data.Models.Operations.Revelations)
                        ? Operations.GetLastNonceRevelations(account, fromLevel, limit)
                        : Task.FromResult(Enumerable.Empty<NonceRevelationOperation>());

                    var delegations = resultOps.HasFlag(Data.Models.Operations.Delegations)
                        ? Operations.GetLastDelegations(account, fromLevel, limit)
                        : Task.FromResult(Enumerable.Empty<DelegationOperation>());

                    var originations = resultOps.HasFlag(Data.Models.Operations.Originations)
                        ? Operations.GetLastOriginations(account, fromLevel, limit)
                        : Task.FromResult(Enumerable.Empty<OriginationOperation>());

                    var transactions = resultOps.HasFlag(Data.Models.Operations.Transactions)
                        ? Operations.GetLastTransactions(account, fromLevel, limit)
                        : Task.FromResult(Enumerable.Empty<TransactionOperation>());

                    var reveals = resultOps.HasFlag(Data.Models.Operations.Reveals)
                        ? Operations.GetLastReveals(account, fromLevel, limit)
                        : Task.FromResult(Enumerable.Empty<RevealOperation>());

                    await Task.WhenAll(
                        endorsements,
                        proposals,
                        ballots,
                        activations,
                        doubleBaking,
                        doubleEndorsing,
                        nonceRevelations,
                        delegations,
                        originations,
                        transactions,
                        reveals);

                    result.AddRange(endorsements.Result);
                    result.AddRange(proposals.Result);
                    result.AddRange(ballots.Result);
                    result.AddRange(activations.Result);
                    result.AddRange(doubleBaking.Result);
                    result.AddRange(doubleEndorsing.Result);
                    result.AddRange(nonceRevelations.Result);
                    result.AddRange(delegations.Result);
                    result.AddRange(originations.Result);
                    result.AddRange(transactions.Result);
                    result.AddRange(reveals.Result);

                    break;
                case RawUser user:

                    var userActivations = resultOps.HasFlag(Data.Models.Operations.Activations)
                        ? Operations.GetLastActivations(account, fromLevel, limit)
                        : Task.FromResult(Enumerable.Empty<ActivationOperation>());

                    var userDelegations = resultOps.HasFlag(Data.Models.Operations.Delegations)
                        ? Operations.GetLastDelegations(account, fromLevel, limit)
                        : Task.FromResult(Enumerable.Empty<DelegationOperation>());

                    var userOriginations = resultOps.HasFlag(Data.Models.Operations.Originations)
                        ? Operations.GetLastOriginations(account, fromLevel, limit)
                        : Task.FromResult(Enumerable.Empty<OriginationOperation>());

                    var userTransactions = resultOps.HasFlag(Data.Models.Operations.Transactions)
                        ? Operations.GetLastTransactions(account, fromLevel, limit)
                        : Task.FromResult(Enumerable.Empty<TransactionOperation>());

                    var userReveals = resultOps.HasFlag(Data.Models.Operations.Reveals)
                        ? Operations.GetLastReveals(account, fromLevel, limit)
                        : Task.FromResult(Enumerable.Empty<RevealOperation>());

                    await Task.WhenAll(
                        userActivations,
                        userDelegations,
                        userOriginations,
                        userTransactions,
                        userReveals);

                    result.AddRange(userActivations.Result);
                    result.AddRange(userDelegations.Result);
                    result.AddRange(userOriginations.Result);
                    result.AddRange(userTransactions.Result);
                    result.AddRange(userReveals.Result);

                    break;
                case RawContract contract:

                    var contractDelegations = resultOps.HasFlag(Data.Models.Operations.Delegations)
                        ? Operations.GetLastDelegations(account, fromLevel, limit)
                        : Task.FromResult(Enumerable.Empty<DelegationOperation>());

                    var contractOriginations = resultOps.HasFlag(Data.Models.Operations.Originations)
                        ? Operations.GetLastOriginations(account, fromLevel, limit)
                        : Task.FromResult(Enumerable.Empty<OriginationOperation>());

                    var contractTransactions = resultOps.HasFlag(Data.Models.Operations.Transactions)
                        ? Operations.GetLastTransactions(account, fromLevel, limit)
                        : Task.FromResult(Enumerable.Empty<TransactionOperation>());

                    var contractReveals = resultOps.HasFlag(Data.Models.Operations.Reveals)
                        ? Operations.GetLastReveals(account, fromLevel, limit)
                        : Task.FromResult(Enumerable.Empty<RevealOperation>());

                    await Task.WhenAll(
                        contractDelegations,
                        contractOriginations,
                        contractTransactions,
                        contractReveals);

                    result.AddRange(contractDelegations.Result);
                    result.AddRange(contractOriginations.Result);
                    result.AddRange(contractTransactions.Result);
                    result.AddRange(contractReveals.Result);

                    break;
            }

            return result.OrderByDescending(x => x.Id).Take(limit);
        }

        string KindToString(int kind) => kind switch
        {
            0 => "delegator_contract",
            1 => "smart_contract",
            _ => "unknown"
        };
    }
}
