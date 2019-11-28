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
                        StakingBalance = delegat.StakingBalance,
                        FirstActivity = delegat.FirstLevel,
                        LastActivity = delegat.LastLevel,
                        NumActivations = delegat.Activated == true ? 1 : 0,
                        NumBallots = delegat.BallotsCount,
                        NumContracts = delegat.Contracts,
                        NumDelegators = delegat.Delegators,
                        NumDelegations = delegat.DelegationsCount,
                        NumDoubleBaking = delegat.DoubleBakingCount,
                        NumDoubleEndorsing = delegat.DoubleEndorsingCount,
                        NumEndorsements = delegat.EndorsementsCount,
                        NumNonceRevelations = delegat.NonceRevelationsCount,
                        NumOriginations = delegat.OriginationsCount,
                        NumProposals = delegat.ProposalsCount,
                        NumReveals = delegat.RevealsCount,
                        NumSystem = delegat.SystemOpsCount,
                        NumTransactions = delegat.TransactionsCount,
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
                        Delegate = user.DelegateId != null ? new DelegateInfo(Aliases[(int)user.DelegateId], user.Staked) : null,
                        NumActivations = user.Activated == true ? 1 : 0,
                        NumContracts = user.Contracts,
                        NumDelegations = user.DelegationsCount,
                        NumOriginations = user.OriginationsCount,
                        NumReveals = user.RevealsCount,
                        NumSystem = user.SystemOpsCount,
                        NumTransactions = user.TransactionsCount
                    };
                case RawContract contract:
                    var creatorAlias = contract.CreatorId != null
                        ? Aliases[(int)contract.CreatorId]
                        : null;

                    var creator = contract.CreatorId != null
                        ? await Accounts.Get(creatorAlias.Address)
                        : null;
                    
                    var managerAlias = contract.ManagerId != null
                        ? Aliases[(int)contract.ManagerId]
                        : null;

                    var manager = contract.ManagerId != null
                        ? (RawUser)await Accounts.Get(managerAlias.Address)
                        : null; ;

                    return new Contract
                    {
                        Kind = KindToString(contract.Kind),
                        Alias = Aliases[contract.Id].Name,
                        Address = address,
                        Balance = contract.Balance,
                        Creator = contract.CreatorId != null ? new CreatorInfo(creatorAlias, creator.Type) : null,
                        Manager = contract.ManagerId != null ? new ManagerInfo(managerAlias, manager.PublicKey, manager.Type) : null,
                        Delegate = contract.DelegateId != null ? new DelegateInfo(Aliases[(int)contract.DelegateId], contract.Staked) : null,
                        FirstActivity = contract.FirstLevel,
                        LastActivity = contract.LastLevel,
                        NumContracts = contract.Contracts,
                        NumDelegations = contract.DelegationsCount,
                        NumOriginations = contract.OriginationsCount,
                        NumReveals = contract.RevealsCount,
                        NumSystem = contract.SystemOpsCount,
                        NumTransactions = contract.TransactionsCount
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
                    delegat.Contracts = await GetContracts(address, 5);
                    delegat.Delegators = await GetDelegators(address, 20);
                    delegat.Operations = await GetOperations(address,
                        Data.Models.Operations.All &
                        ~Data.Models.Operations.Endorsements &
                        ~Data.Models.Operations.Revelations, 20);
                    break;
                case User user when user.FirstActivity != null:
                    user.Contracts = await GetContracts(address, 5);
                    user.Operations = await GetOperations(address,
                        Data.Models.Operations.Manager |
                        Data.Models.Operations.Activations, 20);
                    break;
                case Contract contract:
                    contract.Contracts = await GetContracts(address, 5);
                    contract.Operations = await GetOperations(address,
                        Data.Models.Operations.Manager, 20);
                    break;
            }

            return account;
        }

        public async Task<IEnumerable<IAccount>> Get(int limit = 100, int offset = 0)
        {
            var sql = @"
                SELECT      *
                FROM        ""Accounts""
                ORDER BY    ""Id""
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
                            Delegate = row.DelegateId != null ? new DelegateInfo(Aliases[row.DelegateId], row.Staked) : null,
                            NumActivations = row.Activated == true ? 1 : 0,
                            NumContracts = row.Contracts,
                            NumDelegations = row.DelegationsCount,
                            NumOriginations = row.OriginationsCount,
                            NumReveals = row.RevealsCount,
                            NumSystem = row.SystemOpsCount,
                            NumTransactions = row.TransactionsCount
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
                            StakingBalance = row.StakingBalance,
                            FirstActivity = row.FirstLevel,
                            LastActivity = row.LastLevel,
                            NumActivations = row.Activated == true ? 1 : 0,
                            NumBallots = row.BallotsCount,
                            NumContracts = row.Contracts,
                            NumDelegators = row.Delegators,
                            NumDelegations = row.DelegationsCount,
                            NumDoubleBaking = row.DoubleBakingCount,
                            NumDoubleEndorsing = row.DoubleEndorsingCount,
                            NumEndorsements = row.EndorsementsCount,
                            NumNonceRevelations = row.NonceRevelationsCount,
                            NumOriginations = row.OriginationsCount,
                            NumProposals = row.ProposalsCount,
                            NumReveals = row.RevealsCount,
                            NumSystem = row.SystemOpsCount,
                            NumTransactions = row.TransactionsCount,
                        });
                        break;
                    case 2:
                        accounts.Add(new Contract
                        {
                            Kind = KindToString(row.Kind),
                            Alias = Aliases[row.Id].Name,
                            Address = row.Address,
                            Balance = row.Balance,
                            Creator = row.CreatorId != null ? new CreatorInfo(Aliases[row.CreatorId], null) : null,
                            Manager = row.ManagerId != null ? new ManagerInfo(Aliases[row.ManagerId], null, null) : null,
                            Delegate = row.DelegateId != null ? new DelegateInfo(Aliases[row.DelegateId], row.Staked) : null,
                            FirstActivity = row.FirstLevel,
                            LastActivity = row.LastLevel,
                            NumContracts = row.Contracts,
                            NumDelegations = row.DelegationsCount,
                            NumOriginations = row.OriginationsCount,
                            NumReveals = row.RevealsCount,
                            NumSystem = row.SystemOpsCount,
                            NumTransactions = row.TransactionsCount
                        });
                        break;
                }
            }

            return accounts;
        }

        public async Task<IEnumerable<Contract>> GetContracts(string address, int limit = 100, int offset = 0)
        {
            var account = await Accounts.Get(address);

            var sql = @"
                SELECT      account.*, manager.""PublicKey"" as ""ManagerPublicKey""
                FROM        ""Accounts"" as account
                LEFT JOIN   ""Accounts"" as manager ON manager.""Id"" = account.""ManagerId""
                WHERE       account.""ManagerId"" = @accountId
                ORDER BY    account.""FirstLevel"" DESC
                OFFSET      @offset
                LIMIT       @limit";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { accountId = account.Id, limit, offset });

            return rows.Select(row => new Contract
            {
                Kind = KindToString(row.Kind),
                Alias = Aliases[row.Id].Name,
                Address = row.Address,
                Balance = row.Balance,
                Creator = row.CreatorId != null ? new CreatorInfo(Aliases[row.CreatorId], null) : null,
                Manager = row.ManagerId != null ? new ManagerInfo(Aliases[row.ManagerId], null, null) : null,
                Delegate = row.DelegateId != null ? new DelegateInfo(Aliases[row.DelegateId], row.Staked) : null,
                FirstActivity = row.FirstLevel,
                LastActivity = row.LastLevel,
                NumContracts = row.Contracts,
                NumDelegations = row.DelegationsCount,
                NumOriginations = row.OriginationsCount,
                NumReveals = row.RevealsCount,
                NumSystem = row.SystemOpsCount,
                NumTransactions = row.TransactionsCount
            });
        }

        public async Task<IEnumerable<DelegatorInfo>> GetDelegators(string address, int limit = 100, int offset = 0)
        {
            var delegat = await Accounts.Get(address);

            var sql = @"
                SELECT      ""Id"", ""Type"", ""Balance"", ""DelegationLevel""
                FROM        ""Accounts""
                WHERE       ""DelegateId"" = @delegateId
                ORDER BY    ""DelegationLevel"" DESC
                OFFSET      @offset
                LIMIT       @limit";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { delegateId = delegat.Id, limit, offset });

            return rows.Select(row => new DelegatorInfo
            {
                Type = TypeToString(row.Type),
                Alias = Aliases[row.Id].Name,
                Address = Aliases[row.Id].Address,
                Balance = row.Balance,
                DelegationLevel = row.DelegationLevel
            });
        }

        public async Task<IEnumerable<IOperation>> GetOperations(string address, Data.Models.Operations operations, int limit = 100)
        {
            var account = await Accounts.Get(address);
            var result = new List<IOperation>(limit * 2);

            switch (account)
            {
                case RawDelegate delegat:

                    var endorsements = operations.HasFlag(Data.Models.Operations.Endorsements) && delegat.EndorsementsCount > 0
                        ? Operations.GetLastEndorsements(account, limit)
                        : Task.FromResult(Enumerable.Empty<EndorsementOperation>());

                    var proposals = operations.HasFlag(Data.Models.Operations.Proposals) && delegat.ProposalsCount > 0
                        ? Operations.GetLastProposals(account, limit)
                        : Task.FromResult(Enumerable.Empty<ProposalOperation>());

                    var ballots = operations.HasFlag(Data.Models.Operations.Ballots) && delegat.BallotsCount > 0
                        ? Operations.GetLastBallots(account, limit)
                        : Task.FromResult(Enumerable.Empty<BallotOperation>());

                    var activations = operations.HasFlag(Data.Models.Operations.Activations) && delegat.Activated == true
                        ? Operations.GetLastActivations(account, limit)
                        : Task.FromResult(Enumerable.Empty<ActivationOperation>());

                    var doubleBaking = operations.HasFlag(Data.Models.Operations.DoubleBakings) && delegat.DoubleBakingCount > 0
                        ? Operations.GetLastDoubleBakings(account, limit)
                        : Task.FromResult(Enumerable.Empty<DoubleBakingOperation>());

                    var doubleEndorsing = operations.HasFlag(Data.Models.Operations.DoubleEndorsings) && delegat.DoubleEndorsingCount > 0
                        ? Operations.GetLastDoubleEndorsings(account, limit)
                        : Task.FromResult(Enumerable.Empty<DoubleEndorsingOperation>());

                    var nonceRevelations = operations.HasFlag(Data.Models.Operations.Revelations) && delegat.NonceRevelationsCount > 0
                        ? Operations.GetLastNonceRevelations(account, limit)
                        : Task.FromResult(Enumerable.Empty<NonceRevelationOperation>());

                    var delegations = operations.HasFlag(Data.Models.Operations.Delegations) && delegat.DelegationsCount > 0
                        ? Operations.GetLastDelegations(account, limit)
                        : Task.FromResult(Enumerable.Empty<DelegationOperation>());

                    var originations = operations.HasFlag(Data.Models.Operations.Originations) && delegat.OriginationsCount > 0
                        ? Operations.GetLastOriginations(account, limit)
                        : Task.FromResult(Enumerable.Empty<OriginationOperation>());

                    var transactions = operations.HasFlag(Data.Models.Operations.Transactions) && delegat.TransactionsCount > 0
                        ? Operations.GetLastTransactions(account, limit)
                        : Task.FromResult(Enumerable.Empty<TransactionOperation>());

                    var reveals = operations.HasFlag(Data.Models.Operations.Reveals) && delegat.RevealsCount > 0
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

                    var userActivations = operations.HasFlag(Data.Models.Operations.Activations) && user.Activated == true
                        ? Operations.GetLastActivations(account, limit)
                        : Task.FromResult(Enumerable.Empty<ActivationOperation>());

                    var userDelegations = operations.HasFlag(Data.Models.Operations.Delegations) && user.DelegationsCount > 0
                        ? Operations.GetLastDelegations(account, limit)
                        : Task.FromResult(Enumerable.Empty<DelegationOperation>());

                    var userOriginations = operations.HasFlag(Data.Models.Operations.Originations) && user.OriginationsCount > 0
                        ? Operations.GetLastOriginations(account, limit)
                        : Task.FromResult(Enumerable.Empty<OriginationOperation>());

                    var userTransactions = operations.HasFlag(Data.Models.Operations.Transactions) && user.TransactionsCount > 0
                        ? Operations.GetLastTransactions(account, limit)
                        : Task.FromResult(Enumerable.Empty<TransactionOperation>());

                    var userReveals = operations.HasFlag(Data.Models.Operations.Reveals) && user.RevealsCount > 0
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

                    var contractDelegations = operations.HasFlag(Data.Models.Operations.Delegations) && contract.DelegationsCount > 0
                        ? Operations.GetLastDelegations(account, limit)
                        : Task.FromResult(Enumerable.Empty<DelegationOperation>());

                    var contractOriginations = operations.HasFlag(Data.Models.Operations.Originations) && contract.OriginationsCount > 0
                        ? Operations.GetLastOriginations(account, limit)
                        : Task.FromResult(Enumerable.Empty<OriginationOperation>());

                    var contractTransactions = operations.HasFlag(Data.Models.Operations.Transactions) && contract.TransactionsCount > 0
                        ? Operations.GetLastTransactions(account, limit)
                        : Task.FromResult(Enumerable.Empty<TransactionOperation>());

                    var contractReveals = operations.HasFlag(Data.Models.Operations.Reveals) && contract.RevealsCount > 0
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
            var result = new List<IOperation>(limit * 2);

            switch (account)
            {
                case RawDelegate delegat:

                    var endorsements = operations.HasFlag(Data.Models.Operations.Endorsements) && delegat.EndorsementsCount > 0
                        ? Operations.GetLastEndorsements(account, fromLevel, limit)
                        : Task.FromResult(Enumerable.Empty<EndorsementOperation>());

                    var proposals = operations.HasFlag(Data.Models.Operations.Proposals) && delegat.ProposalsCount > 0
                        ? Operations.GetLastProposals(account, fromLevel, limit)
                        : Task.FromResult(Enumerable.Empty<ProposalOperation>());

                    var ballots = operations.HasFlag(Data.Models.Operations.Ballots) && delegat.BallotsCount > 0
                        ? Operations.GetLastBallots(account, fromLevel, limit)
                        : Task.FromResult(Enumerable.Empty<BallotOperation>());

                    var activations = operations.HasFlag(Data.Models.Operations.Activations) && delegat.Activated == true
                        ? Operations.GetLastActivations(account, fromLevel, limit)
                        : Task.FromResult(Enumerable.Empty<ActivationOperation>());

                    var doubleBaking = operations.HasFlag(Data.Models.Operations.DoubleBakings) && delegat.DoubleBakingCount > 0
                        ? Operations.GetLastDoubleBakings(account, fromLevel, limit)
                        : Task.FromResult(Enumerable.Empty<DoubleBakingOperation>());

                    var doubleEndorsing = operations.HasFlag(Data.Models.Operations.DoubleEndorsings) && delegat.DoubleEndorsingCount > 0
                        ? Operations.GetLastDoubleEndorsings(account, fromLevel, limit)
                        : Task.FromResult(Enumerable.Empty<DoubleEndorsingOperation>());

                    var nonceRevelations = operations.HasFlag(Data.Models.Operations.Revelations) && delegat.NonceRevelationsCount > 0
                        ? Operations.GetLastNonceRevelations(account, fromLevel, limit)
                        : Task.FromResult(Enumerable.Empty<NonceRevelationOperation>());

                    var delegations = operations.HasFlag(Data.Models.Operations.Delegations) && delegat.DelegationsCount > 0
                        ? Operations.GetLastDelegations(account, fromLevel, limit)
                        : Task.FromResult(Enumerable.Empty<DelegationOperation>());

                    var originations = operations.HasFlag(Data.Models.Operations.Originations) && delegat.OriginationsCount > 0
                        ? Operations.GetLastOriginations(account, fromLevel, limit)
                        : Task.FromResult(Enumerable.Empty<OriginationOperation>());

                    var transactions = operations.HasFlag(Data.Models.Operations.Transactions) && delegat.TransactionsCount > 0
                        ? Operations.GetLastTransactions(account, fromLevel, limit)
                        : Task.FromResult(Enumerable.Empty<TransactionOperation>());

                    var reveals = operations.HasFlag(Data.Models.Operations.Reveals) && delegat.RevealsCount > 0
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

                    var userActivations = operations.HasFlag(Data.Models.Operations.Activations) && user.Activated == true
                        ? Operations.GetLastActivations(account, fromLevel, limit)
                        : Task.FromResult(Enumerable.Empty<ActivationOperation>());

                    var userDelegations = operations.HasFlag(Data.Models.Operations.Delegations) && user.DelegationsCount > 0
                        ? Operations.GetLastDelegations(account, fromLevel, limit)
                        : Task.FromResult(Enumerable.Empty<DelegationOperation>());

                    var userOriginations = operations.HasFlag(Data.Models.Operations.Originations) && user.OriginationsCount > 0
                        ? Operations.GetLastOriginations(account, fromLevel, limit)
                        : Task.FromResult(Enumerable.Empty<OriginationOperation>());

                    var userTransactions = operations.HasFlag(Data.Models.Operations.Transactions) && user.TransactionsCount > 0
                        ? Operations.GetLastTransactions(account, fromLevel, limit)
                        : Task.FromResult(Enumerable.Empty<TransactionOperation>());

                    var userReveals = operations.HasFlag(Data.Models.Operations.Reveals) && user.RevealsCount > 0
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

                    var contractDelegations = operations.HasFlag(Data.Models.Operations.Delegations) && contract.DelegationsCount > 0
                        ? Operations.GetLastDelegations(account, fromLevel, limit)
                        : Task.FromResult(Enumerable.Empty<DelegationOperation>());

                    var contractOriginations = operations.HasFlag(Data.Models.Operations.Originations) && contract.OriginationsCount > 0
                        ? Operations.GetLastOriginations(account, fromLevel, limit)
                        : Task.FromResult(Enumerable.Empty<OriginationOperation>());

                    var contractTransactions = operations.HasFlag(Data.Models.Operations.Transactions) && contract.TransactionsCount > 0
                        ? Operations.GetLastTransactions(account, fromLevel, limit)
                        : Task.FromResult(Enumerable.Empty<TransactionOperation>());

                    var contractReveals = operations.HasFlag(Data.Models.Operations.Reveals) && contract.RevealsCount > 0
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

        string TypeToString(int type) => type switch
        {
            0 => "user",
            1 => "delegate",
            2 => "contract",
            _ => "unknown"
        };

        string KindToString(int kind) => kind switch
        {
            0 => "delegator_contract",
            1 => "smart_contract",
            _ => "unknown"
        };
    }
}
