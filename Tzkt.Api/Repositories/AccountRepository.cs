using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Dapper;

using Tzkt.Api.Models;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Repositories
{
    public partial class AccountRepository : DbConnection
    {
        #region static
        const string AliasQuery = @"""Metadata""#>>'{profile,alias}' as ""Alias""";
        #endregion

        readonly AccountsCache Accounts;
        readonly StateCache State;
        readonly TimeCache Time;
        readonly OperationRepository Operations;
        readonly SoftwareCache Software;

        public AccountRepository(AccountsCache accounts, StateCache state, TimeCache time, OperationRepository operations, SoftwareCache software, IConfiguration config) : base(config)
        {
            Accounts = accounts;
            State = state;
            Time = time;
            Operations = operations;
            Software = software;
        }

        public Task<RawAccount> GetRawAsync(string address)
        {
            return Accounts.GetAsync(address);
        }

        public async Task<Account> Get(string address, bool metadata)
        {
            var rawAccount = await Accounts.GetAsync(address);
            if (rawAccount == null)
                return address[0] != 't' || address[1] != 'z' ? null : new EmptyAccount
                {
                    Address = address,
                    Counter = State.Current.ManagerCounter,
                };

            switch (rawAccount)
            {
                case RawDelegate delegat:
                    #region build delegate
                    return new Models.Delegate
                    {
                        Id = delegat.Id,
                        Alias = delegat.Alias,
                        Active = delegat.Staked,
                        Address = delegat.Address,
                        PublicKey = delegat.PublicKey,
                        Revealed = delegat.Revealed,
                        Balance = delegat.Balance,
                        RollupBonds = delegat.RollupBonds,
                        RollupsCount = delegat.RollupsCount,
                        TransferTicketCount = delegat.TransferTicketCount,
                        TxRollupCommitCount = delegat.TxRollupCommitCount,
                        TxRollupDispatchTicketsCount = delegat.TxRollupDispatchTicketsCount,
                        TxRollupFinalizeCommitmentCount = delegat.TxRollupFinalizeCommitmentCount,
                        TxRollupOriginationCount = delegat.TxRollupOriginationCount,
                        TxRollupRejectionCount = delegat.TxRollupRejectionCount,
                        TxRollupRemoveCommitmentCount = delegat.TxRollupRemoveCommitmentCount,
                        TxRollupReturnBondCount = delegat.TxRollupReturnBondCount,
                        TxRollupSubmitBatchCount = delegat.TxRollupSubmitBatchCount,
                        IncreasePaidStorageCount = delegat.IncreasePaidStorageCount,
                        VdfRevelationsCount = delegat.VdfRevelationsCount,
                        FrozenDeposit = delegat.FrozenDeposit,
                        FrozenDepositLimit = delegat.FrozenDepositLimit,
                        DelegatedBalance = delegat.DelegatedBalance,
                        Counter = delegat.Counter,
                        ActivationLevel = delegat.ActivationLevel,
                        ActivationTime = Time[delegat.ActivationLevel],
                        DeactivationLevel = delegat.Staked ? null : delegat.DeactivationLevel,
                        DeactivationTime = delegat.Staked ? null : Time[delegat.DeactivationLevel],
                        StakingBalance = delegat.StakingBalance,
                        FirstActivity = delegat.FirstLevel,
                        FirstActivityTime = Time[delegat.FirstLevel],
                        LastActivity = delegat.LastLevel,
                        LastActivityTime = Time[delegat.LastLevel],
                        NumActivations = delegat.Activated == true ? 1 : 0,
                        NumBallots = delegat.BallotsCount,
                        NumContracts = delegat.ContractsCount,
                        ActiveTokensCount = delegat.ActiveTokensCount,
                        TokenBalancesCount = delegat.TokenBalancesCount,
                        TokenTransfersCount = delegat.TokenTransfersCount,
                        NumDelegators = delegat.DelegatorsCount,
                        NumBlocks = delegat.BlocksCount,
                        NumDelegations = delegat.DelegationsCount,
                        NumDoubleBaking = delegat.DoubleBakingCount,
                        NumDoubleEndorsing = delegat.DoubleEndorsingCount,
                        NumDoublePreendorsing = delegat.DoublePreendorsingCount,
                        NumEndorsements = delegat.EndorsementsCount,
                        NumPreendorsements = delegat.PreendorsementsCount,
                        NumNonceRevelations = delegat.NonceRevelationsCount,
                        NumRevelationPenalties = delegat.RevelationPenaltiesCount,
                        NumEndorsingRewards = delegat.EndorsingRewardsCount,
                        NumOriginations = delegat.OriginationsCount,
                        NumProposals = delegat.ProposalsCount,
                        NumReveals = delegat.RevealsCount,
                        NumRegisterConstants = delegat.RegisterConstantsCount,
                        NumSetDepositsLimits = delegat.SetDepositsLimitsCount,
                        NumMigrations = delegat.MigrationsCount,
                        NumTransactions = delegat.TransactionsCount,
                        Metadata = metadata ? delegat.Metadata : null,
                        Software = delegat.SoftwareId == null ? null : Software[(int)delegat.SoftwareId]
                    };
                    #endregion
                case RawUser user:
                    #region build user
                    var userDelegate = user.DelegateId == null ? null
                        : await Accounts.GetAsync((int)user.DelegateId);

                    return new User
                    {
                        Id = user.Id,
                        Alias = user.Alias,
                        Address = user.Address,
                        Balance = user.Balance,
                        RollupBonds = user.RollupBonds,
                        RollupsCount = user.RollupsCount,
                        TransferTicketCount = user.TransferTicketCount,
                        TxRollupCommitCount = user.TxRollupCommitCount,
                        TxRollupDispatchTicketsCount = user.TxRollupDispatchTicketsCount,
                        TxRollupFinalizeCommitmentCount = user.TxRollupFinalizeCommitmentCount,
                        TxRollupOriginationCount = user.TxRollupOriginationCount,
                        TxRollupRejectionCount = user.TxRollupRejectionCount,
                        TxRollupRemoveCommitmentCount = user.TxRollupRemoveCommitmentCount,
                        TxRollupReturnBondCount = user.TxRollupReturnBondCount,
                        TxRollupSubmitBatchCount = user.TxRollupSubmitBatchCount,
                        IncreasePaidStorageCount = user.IncreasePaidStorageCount,
                        Counter = user.Balance > 0 ? user.Counter : State.Current.ManagerCounter,
                        FirstActivity = user.FirstLevel,
                        FirstActivityTime = Time[user.FirstLevel],
                        LastActivity = user.LastLevel,
                        LastActivityTime = Time[user.LastLevel],
                        PublicKey = user.PublicKey,
                        Revealed = user.Revealed,
                        Delegate = userDelegate == null ? null : new DelegateInfo
                        {
                            Alias = userDelegate.Alias,
                            Address = userDelegate.Address,
                            Active = userDelegate.Staked
                        },
                        DelegationLevel = userDelegate == null ? null : user.DelegationLevel,
                        DelegationTime = userDelegate == null ? null : Time[(int)user.DelegationLevel],
                        NumActivations = user.Activated == true ? 1 : 0,
                        NumContracts = user.ContractsCount,
                        ActiveTokensCount = user.ActiveTokensCount,
                        TokenBalancesCount = user.TokenBalancesCount,
                        TokenTransfersCount = user.TokenTransfersCount,
                        NumDelegations = user.DelegationsCount,
                        NumOriginations = user.OriginationsCount,
                        NumReveals = user.RevealsCount,
                        NumRegisterConstants = user.RegisterConstantsCount,
                        NumSetDepositsLimits = user.SetDepositsLimitsCount,
                        NumMigrations = user.MigrationsCount,
                        NumTransactions = user.TransactionsCount,
                        Metadata = metadata ? user.Metadata : null
                    };
                    #endregion
                case RawContract contract:
                    #region build contract
                    var creator = contract.CreatorId == null ? null
                        : await Accounts.GetAsync((int)contract.CreatorId);

                    var manager = contract.ManagerId == null ? null
                        : (RawUser)await Accounts.GetAsync((int)contract.ManagerId);

                    var contractDelegate = contract.DelegateId == null ? null
                        : await Accounts.GetAsync((int)contract.DelegateId);

                    return new Contract
                    {
                        Id = contract.Id,
                        Alias = contract.Alias,
                        Address = contract.Address,
                        Kind = ContractKinds.ToString(contract.Kind),
                        Tzips = ContractTags.ToList((Data.Models.ContractTags)contract.Tags),
                        Balance = contract.Balance,
                        TransferTicketCount = contract.TransferTicketCount,
                        IncreasePaidStorageCount = contract.IncreasePaidStorageCount,
                        Creator = creator == null ? null : new CreatorInfo
                        {
                            Alias = creator.Alias,
                            Address = creator.Address
                        },
                        Manager = manager == null ? null : new ManagerInfo
                        {
                            Alias = manager.Alias,
                            Address = manager.Address,
                            PublicKey = manager.PublicKey,
                        },
                        Delegate = contractDelegate == null ? null : new DelegateInfo
                        {
                            Alias = contractDelegate.Alias,
                            Address = contractDelegate.Address,
                            Active = contractDelegate.Staked
                        },
                        DelegationLevel = contractDelegate == null ? null : contract.DelegationLevel,
                        DelegationTime = contractDelegate == null ? null : Time[(int)contract.DelegationLevel],
                        FirstActivity = contract.FirstLevel,
                        FirstActivityTime = Time[contract.FirstLevel],
                        LastActivity = contract.LastLevel,
                        LastActivityTime = Time[contract.LastLevel],
                        NumContracts = contract.ContractsCount,
                        ActiveTokensCount = contract.ActiveTokensCount,
                        TokensCount = contract.TokensCount,
                        TokenBalancesCount = contract.TokenBalancesCount,
                        TokenTransfersCount = contract.TokenTransfersCount,
                        NumDelegations = contract.DelegationsCount,
                        NumOriginations = contract.OriginationsCount,
                        NumReveals = contract.RevealsCount,
                        NumMigrations = contract.MigrationsCount,
                        NumTransactions = contract.TransactionsCount,
                        EventsCount = contract.EventsCount,
                        TypeHash = contract.TypeHash,
                        CodeHash = contract.CodeHash,
                        Metadata = metadata ? contract.Metadata : null
                    };
                #endregion
                case RawRollup rollup:
                    #region build rollup
                    return new Rollup
                    {
                        Id = rollup.Id,
                        Alias = rollup.Alias,
                        Address = rollup.Address,
                        Creator = await Accounts.GetAliasAsync(rollup.CreatorId),
                        RollupBonds = rollup.RollupBonds,
                        TransferTicketCount = rollup.TransferTicketCount,
                        TxRollupCommitCount = rollup.TxRollupCommitCount,
                        TxRollupDispatchTicketsCount = rollup.TxRollupDispatchTicketsCount,
                        TxRollupFinalizeCommitmentCount = rollup.TxRollupFinalizeCommitmentCount,
                        TxRollupOriginationCount = rollup.TxRollupOriginationCount,
                        TxRollupRejectionCount = rollup.TxRollupRejectionCount,
                        TxRollupRemoveCommitmentCount = rollup.TxRollupRemoveCommitmentCount,
                        TxRollupReturnBondCount = rollup.TxRollupReturnBondCount,
                        TxRollupSubmitBatchCount = rollup.TxRollupSubmitBatchCount,
                        FirstActivity = rollup.FirstLevel,
                        FirstActivityTime = Time[rollup.FirstLevel],
                        LastActivity = rollup.LastLevel,
                        LastActivityTime = Time[rollup.LastLevel],
                        ActiveTokensCount = rollup.ActiveTokensCount,
                        TokenBalancesCount = rollup.TokenBalancesCount,
                        TokenTransfersCount = rollup.TokenTransfersCount,
                        NumTransactions = rollup.TransactionsCount,
                        Metadata = metadata ? rollup.Metadata : null
                    };
                #endregion
                case RawAccount ghost:
                    #region build ghost
                    return new Ghost
                    {
                        Id = ghost.Id,
                        Alias = ghost.Alias,
                        Address = ghost.Address,
                        ActiveTokensCount = ghost.ActiveTokensCount,
                        TokenBalancesCount = ghost.TokenBalancesCount,
                        TokenTransfersCount = ghost.TokenTransfersCount,
                        FirstActivity = ghost.FirstLevel,
                        FirstActivityTime = Time[ghost.FirstLevel],
                        LastActivity = ghost.LastLevel,
                        LastActivityTime = Time[ghost.LastLevel],
                        Metadata = metadata ? ghost.Metadata : null
                    };
                #endregion
                default:
                    throw new Exception($"Invalid raw account type");
            }
        }

        public async Task<int> GetCount(AccountTypeParameter type, ContractKindParameter kind, Int64Parameter balance, BoolParameter staked, Int32Parameter firstActivity)
        {
            var sql = new SqlBuilder(@"SELECT COUNT(*) FROM ""Accounts""")
                .Filter("Type", type)
                .Filter("Kind", kind)
                .Filter("Balance", balance)
                .Filter("Staked", staked)
                .Filter("FirstLevel", firstActivity);

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<Account>> Get(
            Int32Parameter id,
            AccountTypeParameter type,
            ContractKindParameter kind,
            AccountParameter @delegate,
            Int64Parameter balance,
            BoolParameter staked,
            Int32Parameter lastActivity,
            SortParameter sort,
            OffsetParameter offset,
            int limit)
        {
            var sql = new SqlBuilder($@"SELECT *, {AliasQuery} FROM ""Accounts""")
                .Filter("Id", id)
                .Filter("Type", type)
                .Filter("Kind", kind)
                .Filter("DelegateId", @delegate)
                .Filter("Balance", balance)
                .Filter("Staked", staked)
                .Filter("LastLevel", lastActivity)
                .Take(sort, offset, limit, x => x switch
                {
                    "balance" => ("Balance", "Balance"),
                    "rollupBonds" => ("RollupBonds", "RollupBonds"),
                    "firstActivity" => ("FirstLevel", "FirstLevel"),
                    "lastActivity" => ("LastLevel", "LastLevel"),
                    "numTransactions" => ("TransactionsCount", "TransactionsCount"),
                    "numContracts" => ("ContractsCount", "ContractsCount"),
                    _ => ("Id", "Id")
                });

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            var accounts = new List<Account>(rows.Count());
            foreach (var row in rows)
            {
                switch ((int)row.Type)
                {
                    case 0:
                        #region build user
                        var userDelegate = row.DelegateId == null ? null
                            : await Accounts.GetAsync((int)row.DelegateId);

                        accounts.Add(new User
                        {
                            Id = row.Id,
                            Alias = row.Alias,
                            Address = row.Address,
                            Balance = row.Balance,
                            RollupBonds = row.RollupBonds,
                            RollupsCount = row.RollupsCount,
                            TransferTicketCount = row.TransferTicketCount,
                            TxRollupCommitCount = row.TxRollupCommitCount,
                            TxRollupDispatchTicketsCount = row.TxRollupDispatchTicketsCount,
                            TxRollupFinalizeCommitmentCount = row.TxRollupFinalizeCommitmentCount,
                            TxRollupOriginationCount = row.TxRollupOriginationCount,
                            TxRollupRejectionCount = row.TxRollupRejectionCount,
                            TxRollupRemoveCommitmentCount = row.TxRollupRemoveCommitmentCount,
                            TxRollupReturnBondCount = row.TxRollupReturnBondCount,
                            TxRollupSubmitBatchCount = row.TxRollupSubmitBatchCount,
                            IncreasePaidStorageCount = row.IncreasePaidStorageCount,
                            Counter = row.Balance > 0 ? row.Counter : State.Current.ManagerCounter,
                            FirstActivity = row.FirstLevel,
                            FirstActivityTime = Time[row.FirstLevel],
                            LastActivity = row.LastLevel,
                            LastActivityTime = Time[row.LastLevel],
                            PublicKey = row.PublicKey,
                            Revealed = row.Revealed,
                            Delegate = userDelegate == null ? null : new DelegateInfo
                            {
                                Alias = userDelegate.Alias,
                                Address = userDelegate.Address,
                                Active = userDelegate.Staked
                            },
                            DelegationLevel = userDelegate == null ? null : row.DelegationLevel,
                            DelegationTime = userDelegate == null ? null : (DateTime?)Time[row.DelegationLevel],
                            NumActivations = row.Activated == true ? 1 : 0,
                            NumContracts = row.ContractsCount,
                            ActiveTokensCount = row.ActiveTokensCount,
                            TokenBalancesCount = row.TokenBalancesCount,
                            TokenTransfersCount = row.TokenTransfersCount,
                            NumDelegations = row.DelegationsCount,
                            NumOriginations = row.OriginationsCount,
                            NumReveals = row.RevealsCount,
                            NumRegisterConstants = row.RegisterConstantsCount,
                            NumSetDepositsLimits = row.SetDepositsLimitsCount,
                            NumMigrations = row.MigrationsCount,
                            NumTransactions = row.TransactionsCount
                        });
                        #endregion
                        break;
                    case 1:
                        #region build delegate
                        accounts.Add(new Models.Delegate
                        {
                            Id = row.Id,
                            Alias = row.Alias,
                            Active = row.Staked,
                            Address = row.Address,
                            PublicKey = row.PublicKey,
                            Revealed = row.Revealed,
                            Balance = row.Balance,
                            RollupBonds = row.RollupBonds,
                            RollupsCount = row.RollupsCount,
                            TransferTicketCount = row.TransferTicketCount,
                            TxRollupCommitCount = row.TxRollupCommitCount,
                            TxRollupDispatchTicketsCount = row.TxRollupDispatchTicketsCount,
                            TxRollupFinalizeCommitmentCount = row.TxRollupFinalizeCommitmentCount,
                            TxRollupOriginationCount = row.TxRollupOriginationCount,
                            TxRollupRejectionCount = row.TxRollupRejectionCount,
                            TxRollupRemoveCommitmentCount = row.TxRollupRemoveCommitmentCount,
                            TxRollupReturnBondCount = row.TxRollupReturnBondCount,
                            TxRollupSubmitBatchCount = row.TxRollupSubmitBatchCount,
                            IncreasePaidStorageCount = row.IncreasePaidStorageCount,
                            VdfRevelationsCount = row.VdfRevelationsCount,
                            FrozenDeposit = row.FrozenDeposit,
                            FrozenDepositLimit = row.FrozenDepositLimit,
                            DelegatedBalance = row.DelegatedBalance,
                            Counter = row.Counter,
                            ActivationLevel = row.ActivationLevel,
                            ActivationTime = Time[row.ActivationLevel],
                            DeactivationLevel = row.Staked ? null : (int?)row.DeactivationLevel,
                            DeactivationTime = row.Staked ? null : (DateTime?)Time[row.DeactivationLevel],
                            StakingBalance = row.StakingBalance,
                            FirstActivity = row.FirstLevel,
                            FirstActivityTime = Time[row.FirstLevel],
                            LastActivity = row.LastLevel,
                            LastActivityTime = Time[row.LastLevel],
                            NumActivations = row.Activated == true ? 1 : 0,
                            NumBallots = row.BallotsCount,
                            NumContracts = row.ContractsCount,
                            ActiveTokensCount = row.ActiveTokensCount,
                            TokenBalancesCount = row.TokenBalancesCount,
                            TokenTransfersCount = row.TokenTransfersCount,
                            NumDelegators = row.DelegatorsCount,
                            NumBlocks = row.BlocksCount,
                            NumDelegations = row.DelegationsCount,
                            NumDoubleBaking = row.DoubleBakingCount,
                            NumDoubleEndorsing = row.DoubleEndorsingCount,
                            NumDoublePreendorsing = row.DoublePreendorsingCount,
                            NumEndorsements = row.EndorsementsCount,
                            NumPreendorsements = row.PreendorsementsCount,
                            NumNonceRevelations = row.NonceRevelationsCount,
                            NumRevelationPenalties = row.RevelationPenaltiesCount,
                            NumEndorsingRewards = row.EndorsingRewardsCount,
                            NumOriginations = row.OriginationsCount,
                            NumProposals = row.ProposalsCount,
                            NumReveals = row.RevealsCount,
                            NumRegisterConstants = row.RegisterConstantsCount,
                            NumSetDepositsLimits = row.SetDepositsLimitsCount,
                            NumMigrations = row.MigrationsCount,
                            NumTransactions = row.TransactionsCount,
                            Software = row.SoftwareId == null ? null : Software[row.SoftwareId]
                        });
                        #endregion
                        break;
                    case 2:
                        #region build contract
                        var creator = row.CreatorId == null ? null
                            : await Accounts.GetAsync((int)row.CreatorId);

                        var manager = row.ManagerId == null ? null
                            : (RawUser)await Accounts.GetAsync((int)row.ManagerId);

                        var contractDelegate = row.DelegateId == null ? null
                            : await Accounts.GetAsync((int)row.DelegateId);

                        accounts.Add(new Contract
                        {
                            Id = row.Id,
                            Alias = row.Alias,
                            Address = row.Address,
                            Kind = ContractKinds.ToString(row.Kind),
                            Tzips = ContractTags.ToList((Data.Models.ContractTags)row.Tags),
                            Balance = row.Balance,
                            TransferTicketCount = row.TransferTicketCount,
                            IncreasePaidStorageCount = row.IncreasePaidStorageCount,
                            Creator = creator == null ? null : new CreatorInfo
                            {
                                Alias = creator.Alias,
                                Address = creator.Address
                            },
                            Manager = manager == null ? null : new ManagerInfo
                            {
                                Alias = manager.Alias,
                                Address = manager.Address,
                                PublicKey = manager.PublicKey,
                            },
                            Delegate = contractDelegate == null ? null : new DelegateInfo
                            {
                                Alias = contractDelegate.Alias,
                                Address = contractDelegate.Address,
                                Active = contractDelegate.Staked
                            },
                            DelegationLevel = contractDelegate == null ? null : row.DelegationLevel,
                            DelegationTime = contractDelegate == null ? null : (DateTime?)Time[row.DelegationLevel],
                            FirstActivity = row.FirstLevel,
                            FirstActivityTime = Time[row.FirstLevel],
                            LastActivity = row.LastLevel,
                            LastActivityTime = Time[row.LastLevel],
                            NumContracts = row.ContractsCount,
                            ActiveTokensCount = row.ActiveTokensCount,
                            TokensCount = row.TokensCount,
                            TokenBalancesCount = row.TokenBalancesCount,
                            TokenTransfersCount = row.TokenTransfersCount,
                            NumDelegations = row.DelegationsCount,
                            NumOriginations = row.OriginationsCount,
                            NumReveals = row.RevealsCount,
                            NumMigrations = row.MigrationsCount,
                            NumTransactions = row.TransactionsCount,
                            EventsCount = row.EventsCount,
                            TypeHash = row.TypeHash,
                            CodeHash = row.CodeHash,
                        });
                        #endregion
                        break;
                    case 3:
                        #region build ghost
                        accounts.Add(new Ghost
                        {
                            Id = row.Id,
                            Alias = row.Alias,
                            Address = row.Address,
                            ActiveTokensCount = row.ActiveTokensCount,
                            TokenBalancesCount = row.TokenBalancesCount,
                            TokenTransfersCount = row.TokenTransfersCount,
                            FirstActivity = row.FirstLevel,
                            FirstActivityTime = Time[row.FirstLevel],
                            LastActivity = row.LastLevel,
                            LastActivityTime = Time[row.LastLevel],
                        });
                        #endregion
                        break;
                    case 4:
                        #region build rollup
                        accounts.Add(new Rollup
                        {
                            Id = row.Id,
                            Alias = row.Alias,
                            Address = row.Address,
                            Creator = await Accounts.GetAliasAsync(row.CreatorId),
                            RollupBonds = row.RollupBonds,
                            TransferTicketCount = row.TransferTicketCount,
                            TxRollupCommitCount = row.TxRollupCommitCount,
                            TxRollupDispatchTicketsCount = row.TxRollupDispatchTicketsCount,
                            TxRollupFinalizeCommitmentCount = row.TxRollupFinalizeCommitmentCount,
                            TxRollupOriginationCount = row.TxRollupOriginationCount,
                            TxRollupRejectionCount = row.TxRollupRejectionCount,
                            TxRollupRemoveCommitmentCount = row.TxRollupRemoveCommitmentCount,
                            TxRollupReturnBondCount = row.TxRollupReturnBondCount,
                            TxRollupSubmitBatchCount = row.TxRollupSubmitBatchCount,
                            FirstActivity = row.FirstLevel,
                            FirstActivityTime = Time[row.FirstLevel],
                            LastActivity = row.LastLevel,
                            LastActivityTime = Time[row.LastLevel],
                            ActiveTokensCount = row.ActiveTokensCount,
                            TokenBalancesCount = row.TokenBalancesCount,
                            TokenTransfersCount = row.TokenTransfersCount,
                            NumTransactions = row.TransactionsCount
                        });
                        break;
                        #endregion
                }
            }

            return accounts;
        }

        public async Task<object[][]> Get(
            Int32Parameter id,
            AccountTypeParameter type,
            ContractKindParameter kind,
            AccountParameter @delegate,
            Int64Parameter balance,
            BoolParameter staked,
            Int32Parameter lastActivity,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string[] fields)
        {
            var columns = new HashSet<string>(fields.Length + 2);
            foreach (var field in fields)
            {
                switch (field)
                {
                    case "id": columns.Add(@"""Id"""); break;
                    case "alias": columns.Add(AliasQuery); break;
                    case "type": columns.Add(@"""Type"""); break;
                    case "active": columns.Add(@"""Staked"""); break;
                    case "address": columns.Add(@"""Address"""); break;
                    case "publicKey": columns.Add(@"""PublicKey"""); break;
                    case "revealed": columns.Add(@"""Revealed"""); break;
                    case "balance": columns.Add(@"""Balance"""); break; 
                    case "frozenDeposit": columns.Add(@"""FrozenDeposit"""); break;
                    case "frozenDepositLimit": columns.Add(@"""FrozenDepositLimit"""); break;
                    case "delegatedBalance": columns.Add(@"""DelegatedBalance"""); break;
                    case "counter": columns.Add(@"""Counter"""); break;
                    case "activationLevel": columns.Add(@"""ActivationLevel"""); break;
                    case "activationTime": columns.Add(@"""ActivationLevel"""); break;
                    case "deactivationLevel": columns.Add(@"""DeactivationLevel"""); columns.Add(@"""Staked"""); break;
                    case "deactivationTime": columns.Add(@"""DeactivationLevel"""); columns.Add(@"""Staked"""); break;
                    case "stakingBalance": columns.Add(@"""StakingBalance"""); break;
                    case "firstActivity": columns.Add(@"""FirstLevel"""); break;
                    case "firstActivityTime": columns.Add(@"""FirstLevel"""); break;
                    case "lastActivity": columns.Add(@"""LastLevel"""); break;
                    case "lastActivityTime": columns.Add(@"""LastLevel"""); break;
                    case "numActivations": columns.Add(@"""Activated"""); break;
                    case "numBallots": columns.Add(@"""BallotsCount"""); break;
                    case "numContracts": columns.Add(@"""ContractsCount"""); break;
                    case "activeTokensCount": columns.Add(@"""ActiveTokensCount"""); break;
                    case "tokenBalancesCount": columns.Add(@"""TokenBalancesCount"""); break;
                    case "tokenTransfersCount": columns.Add(@"""TokenTransfersCount"""); break;
                    case "numDelegators": columns.Add(@"""DelegatorsCount"""); break;
                    case "numBlocks": columns.Add(@"""BlocksCount"""); break;
                    case "numDelegations": columns.Add(@"""DelegationsCount"""); break;
                    case "numDoubleBaking": columns.Add(@"""DoubleBakingCount"""); break;
                    case "numDoubleEndorsing": columns.Add(@"""DoubleEndorsingCount"""); break;
                    case "numDoublePreendorsing": columns.Add(@"""DoublePreendorsingCount"""); break;
                    case "numEndorsements": columns.Add(@"""EndorsementsCount"""); break;
                    case "numPreendorsements": columns.Add(@"""PreendorsementsCount"""); break;
                    case "numNonceRevelations": columns.Add(@"""NonceRevelationsCount"""); break;
                    case "numRevelationPenalties": columns.Add(@"""RevelationPenaltiesCount"""); break;
                    case "numEndorsingRewards": columns.Add(@"""EndorsingRewardsCount"""); break;
                    case "numOriginations": columns.Add(@"""OriginationsCount"""); break;
                    case "numProposals": columns.Add(@"""ProposalsCount"""); break;
                    case "numReveals": columns.Add(@"""RevealsCount"""); break;
                    case "numRegisterConstants": columns.Add(@"""RegisterConstantsCount"""); break;
                    case "numSetDepositsLimits": columns.Add(@"""SetDepositsLimitsCount"""); break;                        
                    case "numMigrations": columns.Add(@"""MigrationsCount"""); break;
                    case "numTransactions": columns.Add(@"""TransactionsCount"""); break;
                    case "software": columns.Add(@"""SoftwareId"""); break;
                    case "rollupBonds": columns.Add(@"""RollupBonds"""); break;
                    case "rollupsCount": columns.Add(@"""RollupsCount"""); break;
                    case "transferTicketCount": columns.Add(@"""TransferTicketCount"""); break;
                    case "txRollupCommitCount": columns.Add(@"""TxRollupCommitCount"""); break;
                    case "txRollupDispatchTicketsCount": columns.Add(@"""TxRollupDispatchTicketsCount"""); break;
                    case "txRollupFinalizeCommitmentCount": columns.Add(@"""TxRollupFinalizeCommitmentCount"""); break;
                    case "txRollupOriginationCount": columns.Add(@"""TxRollupOriginationCount"""); break;
                    case "txRollupRejectionCount": columns.Add(@"""TxRollupRejectionCount"""); break;
                    case "txRollupRemoveCommitmentCount": columns.Add(@"""TxRollupRemoveCommitmentCount"""); break;
                    case "txRollupReturnBondCount": columns.Add(@"""TxRollupReturnBondCount"""); break;
                    case "txRollupSubmitBatchCount": columns.Add(@"""TxRollupSubmitBatchCount"""); break;
                    case "vdfRevelationsCount": columns.Add(@"""VdfRevelationsCount"""); break;
                    case "increasePaidStorageCount": columns.Add(@"""IncreasePaidStorageCount"""); break;

                    case "delegate": columns.Add(@"""DelegateId"""); break;
                    case "delegationLevel": columns.Add(@"""DelegationLevel"""); columns.Add(@"""DelegateId"""); break;
                    case "delegationTime": columns.Add(@"""DelegationLevel"""); columns.Add(@"""DelegateId"""); break;

                    case "kind": columns.Add(@"""Kind"""); break;
                    case "tzips": columns.Add(@"""Tags"""); break;
                    case "creator": columns.Add(@"""CreatorId"""); break;
                    case "manager": columns.Add(@"""ManagerId"""); break;
                    case "tokensCount": columns.Add(@"""TokensCount"""); break;
                    case "eventsCount": columns.Add(@"""EventsCount"""); break;
                }
            }

            if (columns.Count == 0)
                return Array.Empty<object[]>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""Accounts""")
                .Filter("Id", id)
                .Filter("Type", type)
                .Filter("Kind", kind)
                .Filter("DelegateId", @delegate)
                .Filter("Balance", balance)
                .Filter("Staked", staked)
                .Filter("LastLevel", lastActivity)
                .Take(sort, offset, limit, x => x switch
                {
                    "balance" => ("Balance", "Balance"),
                    "rollupBonds" => ("RollupBonds", "RollupBonds"),
                    "firstActivity" => ("FirstLevel", "FirstLevel"),
                    "lastActivity" => ("LastLevel", "LastLevel"),
                    "numTransactions" => ("TransactionsCount", "TransactionsCount"),
                    "numContracts" => ("ContractsCount", "ContractsCount"),
                    _ => ("Id", "Id")
                });

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            var result = new object[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object[fields.Length];

            for (int i = 0, j = 0; i < fields.Length; j = 0, i++)
            {
                switch (fields[i])
                {
                    case "id":
                        foreach (var row in rows)
                            result[j++][i] = row.Id;
                        break;
                    case "alias":
                        foreach (var row in rows)
                            result[j++][i] = row.Alias;
                        break;
                    case "type":
                        foreach (var row in rows)
                            result[j++][i] = AccountTypes.ToString(row.Type);
                        break;
                    case "active":
                        foreach (var row in rows)
                            result[j++][i] = row.Staked;
                        break;
                    case "address":
                        foreach (var row in rows)
                            result[j++][i] = row.Address;
                        break;
                    case "publicKey":
                        foreach (var row in rows)
                            result[j++][i] = row.PublicKey;
                        break;
                    case "revealed":
                        foreach (var row in rows)
                            result[j++][i] = row.Revealed;
                        break;
                    case "balance":
                        foreach (var row in rows)
                            result[j++][i] = row.Balance; 
                        break;
                    case "frozenDeposit":
                        foreach (var row in rows)
                            result[j++][i] = row.FrozenDeposit;
                        break;
                    case "frozenDepositLimit":
                        foreach (var row in rows)
                            result[j++][i] = row.FrozenDepositLimit;
                        break;
                    case "delegatedBalance":
                        foreach (var row in rows)
                            result[j++][i] = row.DelegatedBalance;
                        break;
                    case "counter":
                        foreach (var row in rows)
                            result[j++][i] = row.Counter;
                        break;
                    case "activationLevel":
                        foreach (var row in rows)
                            result[j++][i] = row.ActivationLevel;
                        break;
                    case "activationTime":
                        foreach (var row in rows)
                            result[j++][i] = row.ActivationLevel == null ? null : Time[row.ActivationLevel];
                        break;
                    case "deactivationLevel":
                        foreach (var row in rows)
                            result[j++][i] = row.Staked ? null : (int?)row.DeactivationLevel;
                        break;
                    case "deactivationTime":
                        foreach (var row in rows)
                            result[j++][i] = row.Staked ? null : (DateTime?)Time[row.DeactivationLevel];
                        break;
                    case "stakingBalance":
                        foreach (var row in rows)
                            result[j++][i] = row.StakingBalance;
                        break;
                    case "firstActivity":
                        foreach (var row in rows)
                            result[j++][i] = row.FirstLevel;
                        break;
                    case "firstActivityTime":
                        foreach (var row in rows)
                            result[j++][i] = Time[row.FirstLevel];
                        break;
                    case "lastActivity":
                        foreach (var row in rows)
                            result[j++][i] = row.LastLevel;
                        break;
                    case "lastActivityTime":
                        foreach (var row in rows)
                            result[j++][i] = Time[row.LastLevel];
                        break;
                    case "numActivations":
                        foreach (var row in rows)
                            result[j++][i] = row.Activated == true ? 1 : 0;
                        break;
                    case "numBallots":
                        foreach (var row in rows)
                            result[j++][i] = row.BallotsCount;
                        break;
                    case "numContracts":
                        foreach (var row in rows)
                            result[j++][i] = row.ContractsCount;
                        break;
                    case "activeTokensCount":
                        foreach (var row in rows)
                            result[j++][i] = row.ActiveTokensCount;
                        break;
                    case "tokenBalancesCount":
                        foreach (var row in rows)
                            result[j++][i] = row.TokenBalancesCount;
                        break;
                    case "tokenTransfersCount":
                        foreach (var row in rows)
                            result[j++][i] = row.TokenTransfersCount;
                        break;
                    case "numDelegators":
                        foreach (var row in rows)
                            result[j++][i] = row.DelegatorsCount;
                        break;
                    case "numBlocks":
                        foreach (var row in rows)
                            result[j++][i] = row.BlocksCount;
                        break;
                    case "numDelegations":
                        foreach (var row in rows)
                            result[j++][i] = row.DelegationsCount;
                        break;
                    case "numDoubleBaking":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleBakingCount;
                        break;
                    case "numDoubleEndorsing":
                        foreach (var row in rows)
                            result[j++][i] = row.DoubleEndorsingCount;
                        break;
                    case "numDoublePreendorsing":
                        foreach (var row in rows)
                            result[j++][i] = row.DoublePreendorsingCount;
                        break;
                    case "numEndorsements":
                        foreach (var row in rows)
                            result[j++][i] = row.EndorsementsCount;
                        break;
                    case "numPreendorsements":
                        foreach (var row in rows)
                            result[j++][i] = row.PreendorsementsCount;
                        break;
                    case "numNonceRevelations":
                        foreach (var row in rows)
                            result[j++][i] = row.NonceRevelationsCount;
                        break;
                    case "numRevelationPenalties":
                        foreach (var row in rows)
                            result[j++][i] = row.RevelationPenaltiesCount;
                        break;
                    case "numEndorsingRewards":
                        foreach (var row in rows)
                            result[j++][i] = row.EndorsingRewardsCount;
                        break;
                    case "numOriginations":
                        foreach (var row in rows)
                            result[j++][i] = row.OriginationsCount;
                        break;
                    case "numProposals":
                        foreach (var row in rows)
                            result[j++][i] = row.ProposalsCount;
                        break;
                    case "numReveals":
                        foreach (var row in rows)
                            result[j++][i] = row.RevealsCount;
                        break;
                    case "numRegisterConstants":
                        foreach (var row in rows)
                            result[j++][i] = row.RegisterConstantsCount;
                        break;
                    case "numSetDepositsLimits":
                        foreach (var row in rows)
                            result[j++][i] = row.SetDepositsLimitsCount;
                        break;
                    case "numMigrations":
                        foreach (var row in rows)
                            result[j++][i] = row.MigrationsCount;
                        break;
                    case "numTransactions":
                        foreach (var row in rows)
                            result[j++][i] = row.TransactionsCount;
                        break;
                    case "software":
                        foreach (var row in rows)
                            result[j++][i] = row.SoftwareId != null ? Software[row.SoftwareId] : null;
                        break;
                    case "rollupBonds":
                        foreach (var row in rows)
                            result[j++][i] = row.RollupBonds;
                        break;
                    case "rollupsCount":
                        foreach (var row in rows)
                            result[j++][i] = row.RollupsCount;
                        break;
                    case "transferTicketCount":
                        foreach (var row in rows)
                            result[j++][i] = row.TransferTicketCount;
                        break;
                    case "txRollupCommitCount":
                        foreach (var row in rows)
                            result[j++][i] = row.TxRollupCommitCount;
                        break;
                    case "txRollupDispatchTicketsCount":
                        foreach (var row in rows)
                            result[j++][i] = row.TxRollupDispatchTicketsCount;
                        break;
                    case "txRollupFinalizeCommitmentCount":
                        foreach (var row in rows)
                            result[j++][i] = row.TxRollupFinalizeCommitmentCount;
                        break;
                    case "txRollupOriginationCount":
                        foreach (var row in rows)
                            result[j++][i] = row.TxRollupOriginationCount;
                        break;
                    case "txRollupRejectionCount":
                        foreach (var row in rows)
                            result[j++][i] = row.TxRollupRejectionCount;
                        break;
                    case "txRollupRemoveCommitmentCount":
                        foreach (var row in rows)
                            result[j++][i] = row.TxRollupRemoveCommitmentCount;
                        break;
                    case "txRollupReturnBondCount":
                        foreach (var row in rows)
                            result[j++][i] = row.TxRollupReturnBondCount;
                        break;
                    case "txRollupSubmitBatchCount":
                        foreach (var row in rows)
                            result[j++][i] = row.TxRollupSubmitBatchCount;
                        break;
                    case "vdfRevelationsCount":
                        foreach (var row in rows)
                            result[j++][i] = row.VdfRevelationsCount;
                        break;
                    case "increasePaidStorageCount":
                        foreach (var row in rows)
                            result[j++][i] = row.IncreasePaidStorageCount;
                        break;
                    case "delegate":
                        foreach (var row in rows)
                        {
                            var delegat = row.DelegateId == null ? null : Accounts.Get((int)row.DelegateId);
                            result[j++][i] = delegat == null ? null : new DelegateInfo
                            {
                                Alias = delegat.Alias,
                                Address = delegat.Address,
                                Active = delegat.Staked
                            };
                        }
                        break;
                    case "delegationLevel":
                        foreach (var row in rows)
                            result[j++][i] = row.DelegateId == null ? null : row.DelegationLevel;
                        break;
                    case "delegationTime":
                        foreach (var row in rows)
                            result[j++][i] = row.DelegateId == null ? null : Time[row.DelegationLevel];
                        break;
                    case "kind":
                        foreach (var row in rows)
                            result[j++][i] = row.Kind == null ? null : ContractKinds.ToString(row.Kind);
                        break;
                    case "tzips":
                        foreach (var row in rows)
                            result[j++][i] = ContractTags.ToList((Data.Models.ContractTags)row.Tags);
                        break;
                    case "creator":
                        foreach (var row in rows)
                        {
                            var creator = row.CreatorId == null ? null : Accounts.Get((int)row.CreatorId);
                            result[j++][i] = creator == null ? null : new CreatorInfo
                            {
                                Alias = creator.Alias,
                                Address = creator.Address
                            };
                        }
                        break;
                    case "manager":
                        foreach (var row in rows)
                        {
                            var manager = row.ManagerId == null ? null : (RawUser)Accounts.Get((int)row.ManagerId);
                            result[j++][i] = manager == null ? null : new ManagerInfo
                            {
                                Alias = manager.Alias,
                                Address = manager.Address,
                                PublicKey = manager.PublicKey,
                            };
                        }
                        break;
                    case "tokensCount":
                        foreach (var row in rows)
                            result[j++][i] = row.TokensCount;
                        break;
                    case "eventsCount":
                        foreach (var row in rows)
                            result[j++][i] = row.EventsCount;
                        break;
                }
            }

            return result;
        }

        public async Task<object[]> Get(
            Int32Parameter id,
            AccountTypeParameter type,
            ContractKindParameter kind,
            AccountParameter @delegate,
            Int64Parameter balance,
            BoolParameter staked,
            Int32Parameter lastActivity,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string field)
        {
            var columns = new HashSet<string>(3);
            switch (field)
            {
                case "id": columns.Add(@"""Id"""); break;
                case "alias": columns.Add(AliasQuery); break;
                case "type": columns.Add(@"""Type"""); break;
                case "active": columns.Add(@"""Staked"""); break;
                case "address": columns.Add(@"""Address"""); break;
                case "publicKey": columns.Add(@"""PublicKey"""); break;
                case "revealed": columns.Add(@"""Revealed"""); break;
                case "balance": columns.Add(@"""Balance"""); break;
                case "frozenDeposit": columns.Add(@"""FrozenDeposit"""); break;
                case "frozenDepositLimit": columns.Add(@"""FrozenDepositLimit"""); break;
                case "delegatedBalance": columns.Add(@"""DelegatedBalance"""); break;
                case "counter": columns.Add(@"""Counter"""); break;
                case "activationLevel": columns.Add(@"""ActivationLevel"""); break;
                case "activationTime": columns.Add(@"""ActivationLevel"""); break;
                case "deactivationLevel": columns.Add(@"""DeactivationLevel"""); columns.Add(@"""Staked"""); break;
                case "deactivationTime": columns.Add(@"""DeactivationLevel"""); columns.Add(@"""Staked"""); break;
                case "stakingBalance": columns.Add(@"""StakingBalance"""); break;
                case "firstActivity": columns.Add(@"""FirstLevel"""); break;
                case "firstActivityTime": columns.Add(@"""FirstLevel"""); break;
                case "lastActivity": columns.Add(@"""LastLevel"""); break;
                case "lastActivityTime": columns.Add(@"""LastLevel"""); break;
                case "numActivations": columns.Add(@"""Activated"""); break;
                case "numBallots": columns.Add(@"""BallotsCount"""); break;
                case "numContracts": columns.Add(@"""ContractsCount"""); break;
                case "activeTokensCount": columns.Add(@"""ActiveTokensCount"""); break;
                case "tokenBalancesCount": columns.Add(@"""TokenBalancesCount"""); break;
                case "tokenTransfersCount": columns.Add(@"""TokenTransfersCount"""); break;
                case "numDelegators": columns.Add(@"""DelegatorsCount"""); break;
                case "numBlocks": columns.Add(@"""BlocksCount"""); break;
                case "numDelegations": columns.Add(@"""DelegationsCount"""); break;
                case "numDoubleBaking": columns.Add(@"""DoubleBakingCount"""); break;
                case "numDoubleEndorsing": columns.Add(@"""DoubleEndorsingCount"""); break;
                case "numDoublePreendorsing": columns.Add(@"""DoublePreendorsingCount"""); break;
                case "numEndorsements": columns.Add(@"""EndorsementsCount"""); break;
                case "numPreendorsements": columns.Add(@"""PreendorsementsCount"""); break;
                case "numNonceRevelations": columns.Add(@"""NonceRevelationsCount"""); break;
                case "numRevelationPenalties": columns.Add(@"""RevelationPenaltiesCount"""); break;
                case "numEndorsingRewards": columns.Add(@"""EndorsingRewardsCount"""); break;
                case "numOriginations": columns.Add(@"""OriginationsCount"""); break;
                case "numProposals": columns.Add(@"""ProposalsCount"""); break;
                case "numReveals": columns.Add(@"""RevealsCount"""); break;
                case "numRegisterConstants": columns.Add(@"""RegisterConstantsCount"""); break;
                case "numSetDepositsLimits": columns.Add(@"""SetDepositsLimitsCount"""); break;
                case "numMigrations": columns.Add(@"""MigrationsCount"""); break;
                case "numTransactions": columns.Add(@"""TransactionsCount"""); break;
                case "software": columns.Add(@"""SoftwareId"""); break;
                case "rollupBonds": columns.Add(@"""RollupBonds"""); break;
                case "rollupsCount": columns.Add(@"""RollupsCount"""); break;
                case "transferTicketCount": columns.Add(@"""TransferTicketCount"""); break;
                case "txRollupCommitCount": columns.Add(@"""TxRollupCommitCount"""); break;
                case "txRollupDispatchTicketsCount": columns.Add(@"""TxRollupDispatchTicketsCount"""); break;
                case "txRollupFinalizeCommitmentCount": columns.Add(@"""TxRollupFinalizeCommitmentCount"""); break;
                case "txRollupOriginationCount": columns.Add(@"""TxRollupOriginationCount"""); break;
                case "txRollupRejectionCount": columns.Add(@"""TxRollupRejectionCount"""); break;
                case "txRollupRemoveCommitmentCount": columns.Add(@"""TxRollupRemoveCommitmentCount"""); break;
                case "txRollupReturnBondCount": columns.Add(@"""TxRollupReturnBondCount"""); break;
                case "txRollupSubmitBatchCount": columns.Add(@"""TxRollupSubmitBatchCount"""); break;
                case "vdfRevelationsCount": columns.Add(@"""VdfRevelationsCount"""); break;
                case "increasePaidStorageCount": columns.Add(@"""IncreasePaidStorageCount"""); break;

                case "delegate": columns.Add(@"""DelegateId"""); break;
                case "delegationLevel": columns.Add(@"""DelegationLevel"""); columns.Add(@"""DelegateId"""); break;
                case "delegationTime": columns.Add(@"""DelegationLevel"""); columns.Add(@"""DelegateId"""); break;

                case "kind": columns.Add(@"""Kind"""); break;
                case "tzips": columns.Add(@"""Tags"""); break;
                case "creator": columns.Add(@"""CreatorId"""); break;
                case "manager": columns.Add(@"""ManagerId"""); break;
                case "tokensCount": columns.Add(@"""TokensCount"""); break;
                case "eventsCount": columns.Add(@"""EventsCount"""); break;
            }

            if (columns.Count == 0)
                return Array.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""Accounts""")
                .Filter("Id", id)
                .Filter("Type", type)
                .Filter("Kind", kind)
                .Filter("DelegateId", @delegate)
                .Filter("Balance", balance)
                .Filter("Staked", staked)
                .Filter("LastLevel", lastActivity)
                .Take(sort, offset, limit, x => x switch
                {
                    "balance" => ("Balance", "Balance"),
                    "rollupBonds" => ("RollupBonds", "RollupBonds"),
                    "firstActivity" => ("FirstLevel", "FirstLevel"),
                    "lastActivity" => ("LastLevel", "LastLevel"),
                    "numTransactions" => ("TransactionsCount", "TransactionsCount"),
                    "numContracts" => ("ContractsCount", "ContractsCount"),
                    _ => ("Id", "Id")
                });

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            var result = new object[rows.Count()];
            var j = 0;

            switch (field)
            {
                case "id":
                    foreach (var row in rows)
                        result[j++] = row.Id;
                    break;
                case "alias":
                    foreach (var row in rows)
                        result[j++] = row.Alias;
                    break;
                case "type":
                    foreach (var row in rows)
                        result[j++] = AccountTypes.ToString(row.Type);
                    break;
                case "active":
                    foreach (var row in rows)
                        result[j++] = row.Staked;
                    break;
                case "address":
                    foreach (var row in rows)
                        result[j++] = row.Address;
                    break;
                case "publicKey":
                    foreach (var row in rows)
                        result[j++] = row.PublicKey;
                    break;
                case "revealed":
                    foreach (var row in rows)
                        result[j++] = row.Revealed;
                    break;
                case "balance":
                    foreach (var row in rows)
                        result[j++] = row.Balance; 
                    break;
                case "frozenDeposit":
                    foreach (var row in rows)
                        result[j++] = row.FrozenDeposit;
                    break;
                case "frozenDepositLimit":
                    foreach (var row in rows)
                        result[j++] = row.FrozenDepositLimit;
                    break;
                case "delegatedBalance":
                    foreach (var row in rows)
                        result[j++] = row.DelegatedBalance;
                    break;
                case "counter":
                    foreach (var row in rows)
                        result[j++] = row.Counter;
                    break;
                case "activationLevel":
                    foreach (var row in rows)
                        result[j++] = row.ActivationLevel;
                    break;
                case "activationTime":
                    foreach (var row in rows)
                        result[j++] = row.ActivationLevel == null ? null : Time[row.ActivationLevel];
                    break;
                case "deactivationLevel":
                    foreach (var row in rows)
                        result[j++] = row.Staked ? null : (int?)row.DeactivationLevel;
                    break;
                case "deactivationTime":
                    foreach (var row in rows)
                        result[j++] = row.Staked ? null : (DateTime?)Time[row.DeactivationLevel];
                    break;
                case "stakingBalance":
                    foreach (var row in rows)
                        result[j++] = row.StakingBalance;
                    break;
                case "firstActivity":
                    foreach (var row in rows)
                        result[j++] = row.FirstLevel;
                    break;
                case "firstActivityTime":
                    foreach (var row in rows)
                        result[j++] = Time[row.FirstLevel];
                    break;
                case "lastActivity":
                    foreach (var row in rows)
                        result[j++] = row.LastLevel;
                    break;
                case "lastActivityTime":
                    foreach (var row in rows)
                        result[j++] = Time[row.LastLevel];
                    break;
                case "numActivations":
                    foreach (var row in rows)
                        result[j++] = row.Activated == true ? 1 : 0;
                    break;
                case "numBallots":
                    foreach (var row in rows)
                        result[j++] = row.BallotsCount;
                    break;
                case "numContracts":
                    foreach (var row in rows)
                        result[j++] = row.ContractsCount;
                    break;
                case "activeTokensCount":
                    foreach (var row in rows)
                        result[j++] = row.ActiveTokensCount;
                    break;
                case "tokenBalancesCount":
                    foreach (var row in rows)
                        result[j++] = row.TokenBalancesCount;
                    break;
                case "tokenTransfersCount":
                    foreach (var row in rows)
                        result[j++] = row.TokenTransfersCount;
                    break;
                case "numDelegators":
                    foreach (var row in rows)
                        result[j++] = row.DelegatorsCount;
                    break;
                case "numBlocks":
                    foreach (var row in rows)
                        result[j++] = row.BlocksCount;
                    break;
                case "numDelegations":
                    foreach (var row in rows)
                        result[j++] = row.DelegationsCount;
                    break;
                case "numDoubleBaking":
                    foreach (var row in rows)
                        result[j++] = row.DoubleBakingCount;
                    break;
                case "numDoubleEndorsing":
                    foreach (var row in rows)
                        result[j++] = row.DoubleEndorsingCount;
                    break;
                case "numDoublePreendorsing":
                    foreach (var row in rows)
                        result[j++] = row.DoublePreendorsingCount;
                    break;
                case "numEndorsements":
                    foreach (var row in rows)
                        result[j++] = row.EndorsementsCount;
                    break;
                case "numPreendorsements":
                    foreach (var row in rows)
                        result[j++] = row.PreendorsementsCount;
                    break;
                case "numNonceRevelations":
                    foreach (var row in rows)
                        result[j++] = row.NonceRevelationsCount;
                    break;
                case "numRevelationPenalties":
                    foreach (var row in rows)
                        result[j++] = row.RevelationPenaltiesCount;
                    break;
                case "numEndorsingRewards":
                    foreach (var row in rows)
                        result[j++] = row.EndorsingRewardsCount;
                    break;
                case "numOriginations":
                    foreach (var row in rows)
                        result[j++] = row.OriginationsCount;
                    break;
                case "numProposals":
                    foreach (var row in rows)
                        result[j++] = row.ProposalsCount;
                    break;
                case "numReveals":
                    foreach (var row in rows)
                        result[j++] = row.RevealsCount;
                    break;
                case "numRegisterConstants":
                    foreach (var row in rows)
                        result[j++] = row.RegisterConstantsCount;
                    break;
                case "numSetDepositsLimits":
                    foreach (var row in rows)
                        result[j++] = row.SetDepositsLimitsCount;
                    break;
                case "numMigrations":
                    foreach (var row in rows)
                        result[j++] = row.MigrationsCount;
                    break;
                case "numTransactions":
                    foreach (var row in rows)
                        result[j++] = row.TransactionsCount;
                    break;
                case "software":
                    foreach (var row in rows)
                        result[j++] = row.SoftwareId != null ? Software[row.SoftwareId] : null;
                    break;
                case "rollupBonds":
                    foreach (var row in rows)
                        result[j++] = row.RollupBonds;
                    break;
                case "rollupsCount":
                    foreach (var row in rows)
                        result[j++] = row.RollupsCount;
                    break;
                case "transferTicketCount":
                    foreach (var row in rows)
                        result[j++] = row.TransferTicketCount;
                    break;
                case "txRollupCommitCount":
                    foreach (var row in rows)
                        result[j++] = row.TxRollupCommitCount;
                    break;
                case "txRollupDispatchTicketsCount":
                    foreach (var row in rows)
                        result[j++] = row.TxRollupDispatchTicketsCount;
                    break;
                case "txRollupFinalizeCommitmentCount":
                    foreach (var row in rows)
                        result[j++] = row.TxRollupFinalizeCommitmentCount;
                    break;
                case "txRollupOriginationCount":
                    foreach (var row in rows)
                        result[j++] = row.TxRollupOriginationCount;
                    break;
                case "txRollupRejectionCount":
                    foreach (var row in rows)
                        result[j++] = row.TxRollupRejectionCount;
                    break;
                case "txRollupRemoveCommitmentCount":
                    foreach (var row in rows)
                        result[j++] = row.TxRollupRemoveCommitmentCount;
                    break;
                case "txRollupReturnBondCount":
                    foreach (var row in rows)
                        result[j++] = row.TxRollupReturnBondCount;
                    break;
                case "txRollupSubmitBatchCount":
                    foreach (var row in rows)
                        result[j++] = row.TxRollupSubmitBatchCount;
                    break;
                case "vdfRevelationsCount":
                    foreach (var row in rows)
                        result[j++] = row.VdfRevelationsCount;
                    break;
                case "increasePaidStorageCount":
                    foreach (var row in rows)
                        result[j++] = row.IncreasePaidStorageCount;
                    break;
                case "delegate":
                    foreach (var row in rows)
                    {
                        var delegat = row.DelegateId == null ? null : Accounts.Get((int)row.DelegateId);
                        result[j++] = delegat == null ? null : new DelegateInfo
                        {
                            Alias = delegat.Alias,
                            Address = delegat.Address,
                            Active = delegat.Staked
                        };
                    }
                    break;
                case "delegationLevel":
                    foreach (var row in rows)
                        result[j++] = row.DelegateId == null ? null : row.DelegationLevel;
                    break;
                case "delegationTime":
                    foreach (var row in rows)
                        result[j++] = row.DelegateId == null ? null : Time[row.DelegationLevel];
                    break;
                case "kind":
                    foreach (var row in rows)
                        result[j++] = row.Kind == null ? null : ContractKinds.ToString(row.Kind);
                    break;
                case "tzips":
                    foreach (var row in rows)
                        result[j++] = ContractTags.ToList((Data.Models.ContractTags)row.Tags);
                    break;
                case "creator":
                    foreach (var row in rows)
                    {
                        var creator = row.CreatorId == null ? null : Accounts.Get((int)row.CreatorId);
                        result[j++] = creator == null ? null : new CreatorInfo
                        {
                            Alias = creator.Alias,
                            Address = creator.Address
                        };
                    }
                    break;
                case "manager":
                    foreach (var row in rows)
                    {
                        var manager = row.ManagerId == null ? null : (RawUser)Accounts.Get((int)row.ManagerId);
                        result[j++] = manager == null ? null : new ManagerInfo
                        {
                            Alias = manager.Alias,
                            Address = manager.Address,
                            PublicKey = manager.PublicKey,
                        };
                    }
                    break;
                case "tokensCount":
                    foreach (var row in rows)
                        result[j++] = row.TokensCount;
                    break;
                case "eventsCount":
                    foreach (var row in rows)
                        result[j++] = row.EventsCount;
                    break;
            }

            return result;
        }

        public async Task<IEnumerable<RelatedContract>> GetRelatedContracts(
            string address,
            SortParameter sort,
            OffsetParameter offset,
            int limit)
        {
            var account = await Accounts.GetAsync(address);
            if (account == null || account.ContractsCount == 0)
                return Enumerable.Empty<RelatedContract>();

            var sql = new SqlBuilder($@"
                SELECT  ""Id"", ""Kind"", ""Address"", ""Balance"", ""DelegateId"", ""FirstLevel"", {AliasQuery}
                FROM ""Accounts""")
                .Filter($@"(""CreatorId"" = {account.Id} OR ""ManagerId"" = {account.Id})")
                .Take(sort ?? new SortParameter { Desc = "id" }, offset, limit, x => x switch
                {
                    "balance" => ("Balance", "Balance"),
                    "creationLevel" => ("Id", "FirstLevel"),
                    _ => ("Id", "Id")
                });

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row =>
            {
                var delegat = row.DelegateId == null ? null
                    : Accounts.Get((int)row.DelegateId);

                return new RelatedContract
                {
                    Kind = ContractKinds.ToString(row.Kind),
                    Alias = row.Alias,
                    Address = row.Address,
                    Balance = row.Balance,
                    Delegate = row.DelegateId == null ? null : new DelegateInfo
                    {
                        Alias = delegat.Alias,
                        Address = delegat.Address,
                        Active = delegat.Staked
                    },
                    CreationLevel = row.FirstLevel,
                    CreationTime = Time[row.FirstLevel]
                };
            });
        }

        public async Task<IEnumerable<Delegator>> GetDelegators(
            string address,
            AccountTypeParameter type,
            Int64Parameter balance,
            Int32Parameter delegationLevel,
            SortParameter sort,
            OffsetParameter offset,
            int limit)
        {
            if (await Accounts.GetAsync(address) is not RawDelegate delegat || delegat.DelegatorsCount == 0)
                return Enumerable.Empty<Delegator>();

            var sql = new SqlBuilder($@"
                SELECT ""Id"", ""Address"", ""Type"", ""Balance"", ""DelegationLevel"", {AliasQuery}
                FROM ""Accounts""")
                .Filter("DelegateId", delegat.Id)
                .Filter("Type", type)
                .Filter("Balance", balance)
                .Filter("DelegationLevel", delegationLevel)
                .Take(sort ?? new SortParameter { Desc = "delegationLevel" }, offset, limit, x => x == "balance" ? ("Balance", "Balance") : ("DelegationLevel", "DelegationLevel"));

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row =>
            {
                return new Delegator
                {
                    Type = AccountTypes.ToString(row.Type),
                    Alias = row.Alias,
                    Address = row.Address,
                    Balance = row.Balance,
                    DelegationLevel = row.DelegationLevel,
                    DelegationTime = Time[row.DelegationLevel]
                };
            });
        }

        public async Task<IEnumerable<Operation>> GetOperations(
            string address,
            HashSet<string> types,
            AccountParameter initiator,
            AccountParameter sender,
            AccountParameter target,
            AccountParameter prevDelegate,
            AccountParameter newDelegate,
            AccountParameter contractManager,
            AccountParameter contractDelegate,
            AccountParameter originatedContract,
            AccountParameter accuser,
            AccountParameter offender,
            AccountParameter baker,
            Int32Parameter level,
            DateTimeParameter timestamp,
            StringParameter entrypoint,
            JsonParameter parameter,
            BoolParameter hasInternals,
            OperationStatusParameter status,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            MichelineFormat format,
            Symbols quote)
        {
            var account = await Accounts.GetAsync(address);
            if (account == null) return Enumerable.Empty<Operation>();
            var result = new List<Operation>(limit * 2);

            switch (account)
            {
                case RawDelegate delegat:
                    var _delegat = new AccountParameter { Eq = delegat.Id };

                    var endorsements = delegat.EndorsementsCount > 0 && types.Contains(OpTypes.Endorsement)
                        ? Operations.GetEndorsements(_delegat, level, timestamp, sort, offset, limit, quote)
                        : Task.FromResult(Enumerable.Empty<EndorsementOperation>());

                    var preendorsements = delegat.PreendorsementsCount > 0 && types.Contains(OpTypes.Preendorsement)
                        ? Operations.GetPreendorsements(_delegat, level, timestamp, sort, offset, limit, quote)
                        : Task.FromResult(Enumerable.Empty<PreendorsementOperation>());

                    var ballots = delegat.BallotsCount > 0 && types.Contains(OpTypes.Ballot)
                        ? Operations.GetBallots(_delegat, level, timestamp, null, null, null, null, sort, offset, limit, quote)
                        : Task.FromResult(Enumerable.Empty<BallotOperation>());

                    var proposals = delegat.ProposalsCount > 0 && types.Contains(OpTypes.Proposal)
                        ? Operations.GetProposals(_delegat, level, timestamp, null, null, null, null, sort, offset, limit, quote)
                        : Task.FromResult(Enumerable.Empty<ProposalOperation>());

                    var activations = delegat.Activated == true && types.Contains(OpTypes.Activation)
                        ? Operations.GetActivations(_delegat, level, timestamp, sort, offset, limit, quote)
                        : Task.FromResult(Enumerable.Empty<ActivationOperation>());

                    var doubleBaking = delegat.DoubleBakingCount > 0 && types.Contains(OpTypes.DoubleBaking)
                        ? Operations.GetDoubleBakings(new AnyOfParameter { Fields = new[] { "accuser", "offender" }, Eq = delegat.Id }, accuser, offender, level, timestamp, sort, offset, limit, quote)
                        : Task.FromResult(Enumerable.Empty<DoubleBakingOperation>());

                    var doubleEndorsing = delegat.DoubleEndorsingCount > 0 && types.Contains(OpTypes.DoubleEndorsing)
                        ? Operations.GetDoubleEndorsings(new AnyOfParameter { Fields = new[] { "accuser", "offender" }, Eq = delegat.Id }, accuser, offender, level, timestamp, sort, offset, limit, quote)
                        : Task.FromResult(Enumerable.Empty<DoubleEndorsingOperation>());

                    var doublePreendorsing = delegat.DoublePreendorsingCount > 0 && types.Contains(OpTypes.DoublePreendorsing)
                        ? Operations.GetDoublePreendorsings(new AnyOfParameter { Fields = new[] { "accuser", "offender" }, Eq = delegat.Id }, accuser, offender, level, timestamp, sort, offset, limit, quote)
                        : Task.FromResult(Enumerable.Empty<DoublePreendorsingOperation>());

                    var nonceRevelations = delegat.NonceRevelationsCount > 0 && types.Contains(OpTypes.NonceRevelation)
                        ? Operations.GetNonceRevelations(new AnyOfParameter { Fields = new[] { "baker", "sender" }, Eq = delegat.Id }, baker, sender, level, null, timestamp, sort, offset, limit, quote)
                        : Task.FromResult(Enumerable.Empty<NonceRevelationOperation>());

                    var vdfRevelations = delegat.VdfRevelationsCount > 0 && types.Contains(OpTypes.VdfRevelation)
                        ? Operations.GetVdfRevelations(baker, level, null, timestamp, sort, offset, limit, quote)
                        : Task.FromResult(Enumerable.Empty<VdfRevelationOperation>());

                    var delegations = delegat.DelegationsCount > 0 && types.Contains(OpTypes.Delegation)
                        ? Operations.GetDelegations(new AnyOfParameter { Fields = new[] { "initiator", "sender", "prevDelegate", "newDelegate" }, Eq = delegat.Id }, initiator, sender, prevDelegate, newDelegate, level, timestamp, null, status, sort, offset, limit, quote)
                        : Task.FromResult(Enumerable.Empty<DelegationOperation>());

                    var originations = delegat.OriginationsCount > 0 && types.Contains(OpTypes.Origination)
                        ? Operations.GetOriginations(new AnyOfParameter { Fields = new[] { "initiator", "sender", "contractManager", "contractDelegate", "originatedContract" }, Eq = delegat.Id }, initiator, sender, contractManager, contractDelegate, originatedContract, null, null, null, level, timestamp, null, null, status, sort, offset, limit, format, quote)
                        : Task.FromResult(Enumerable.Empty<OriginationOperation>());

                    var transactions = delegat.TransactionsCount > 0 && types.Contains(OpTypes.Transaction)
                        ? Operations.GetTransactions(new AnyOfParameter { Fields = new[] { "initiator", "sender", "target" }, Eq = delegat.Id }, initiator, sender, target, null, null, level, timestamp, null, null, null, entrypoint, parameter, hasInternals, status, sort, offset, limit, format, quote)
                        : Task.FromResult(Enumerable.Empty<TransactionOperation>());

                    var reveals = delegat.RevealsCount > 0 && types.Contains(OpTypes.Reveal)
                        ? Operations.GetReveals(_delegat, level, timestamp, status, sort, offset, limit, quote)
                        : Task.FromResult(Enumerable.Empty<RevealOperation>());

                    var registerConstants = delegat.RegisterConstantsCount > 0 && types.Contains(OpTypes.RegisterConstant)
                        ? Operations.GetRegisterConstants(_delegat, null, level, timestamp, status, sort, offset, limit, format, quote)
                        : Task.FromResult(Enumerable.Empty<RegisterConstantOperation>());

                    var setDepositsLimits = delegat.SetDepositsLimitsCount > 0 && types.Contains(OpTypes.SetDepositsLimit)
                        ? Operations.GetSetDepositsLimits(_delegat, level, timestamp, status, sort, offset, limit, quote)
                        : Task.FromResult(Enumerable.Empty<SetDepositsLimitOperation>());

                    var transferTicketOps = delegat.TransferTicketCount > 0 && types.Contains(OpTypes.TransferTicket)
                        ? Operations.GetTransferTicketOps(null, _delegat, null, null, level, timestamp, status, sort, offset, limit, format, quote)
                        : Task.FromResult(Enumerable.Empty<TransferTicketOperation>());

                    var txRollupCommitOps = delegat.TxRollupCommitCount > 0 && types.Contains(OpTypes.TxRollupCommit)
                        ? Operations.GetTxRollupCommitOps(_delegat, null, level, timestamp, status, sort, offset, limit, quote)
                        : Task.FromResult(Enumerable.Empty<TxRollupCommitOperation>());

                    var txRollupDispatchTicketsOps = delegat.TxRollupDispatchTicketsCount > 0 && types.Contains(OpTypes.TxRollupDispatchTickets)
                        ? Operations.GetTxRollupDispatchTicketsOps(_delegat, null, level, timestamp, status, sort, offset, limit, quote)
                        : Task.FromResult(Enumerable.Empty<TxRollupDispatchTicketsOperation>());

                    var txRollupFinalizeCommitmentOps = delegat.TxRollupFinalizeCommitmentCount > 0 && types.Contains(OpTypes.TxRollupFinalizeCommitment)
                        ? Operations.GetTxRollupFinalizeCommitmentOps(_delegat, null, level, timestamp, status, sort, offset, limit, quote)
                        : Task.FromResult(Enumerable.Empty<TxRollupFinalizeCommitmentOperation>());

                    var txRollupOriginationOps = delegat.TxRollupOriginationCount > 0 && types.Contains(OpTypes.TxRollupOrigination)
                        ? Operations.GetTxRollupOriginationOps(_delegat, null, level, timestamp, status, sort, offset, limit, quote)
                        : Task.FromResult(Enumerable.Empty<TxRollupOriginationOperation>());

                    var txRollupRejectionOps = delegat.TxRollupRejectionCount > 0 && types.Contains(OpTypes.TxRollupRejection)
                        ? Operations.GetTxRollupRejectionOps(new AnyOfParameter { Fields = new[] { "sender", "committer" }, Eq = delegat.Id }, null, null, null, level, timestamp, status, sort, offset, limit, quote)
                        : Task.FromResult(Enumerable.Empty<TxRollupRejectionOperation>());

                    var txRollupRemoveCommitmentOps = delegat.TxRollupRemoveCommitmentCount > 0 && types.Contains(OpTypes.TxRollupRemoveCommitment)
                        ? Operations.GetTxRollupRemoveCommitmentOps(_delegat, null, level, timestamp, status, sort, offset, limit, quote)
                        : Task.FromResult(Enumerable.Empty<TxRollupRemoveCommitmentOperation>());

                    var txRollupReturnBondOps = delegat.TxRollupReturnBondCount > 0 && types.Contains(OpTypes.TxRollupReturnBond)
                        ? Operations.GetTxRollupReturnBondOps(_delegat, null, level, timestamp, status, sort, offset, limit, quote)
                        : Task.FromResult(Enumerable.Empty<TxRollupReturnBondOperation>());

                    var txRollupSubmitBatchOps = delegat.TxRollupSubmitBatchCount > 0 && types.Contains(OpTypes.TxRollupSubmitBatch)
                        ? Operations.GetTxRollupSubmitBatchOps(_delegat, null, level, timestamp, status, sort, offset, limit, quote)
                        : Task.FromResult(Enumerable.Empty<TxRollupSubmitBatchOperation>());

                    var increasePaidStorageOps = delegat.IncreasePaidStorageCount > 0 && types.Contains(OpTypes.IncreasePaidStorage)
                        ? Operations.GetIncreasePaidStorageOps(_delegat, null, level, timestamp, status, sort, offset, limit, quote)
                        : Task.FromResult(Enumerable.Empty<IncreasePaidStorageOperation>());

                    var migrations = delegat.MigrationsCount > 0 && types.Contains(OpTypes.Migration)
                        ? Operations.GetMigrations(_delegat, null, null, null, level, timestamp, sort, offset, limit, format, quote)
                        : Task.FromResult(Enumerable.Empty<MigrationOperation>());

                    var revelationPenalties = delegat.RevelationPenaltiesCount > 0 && types.Contains(OpTypes.RevelationPenalty)
                        ? Operations.GetRevelationPenalties(_delegat, level, timestamp, sort, offset, limit, quote)
                        : Task.FromResult(Enumerable.Empty<RevelationPenaltyOperation>());

                    var bakingOps = delegat.BlocksCount > 0 && types.Contains(OpTypes.Baking)
                        ? Operations.GetBakings(new AnyOfParameter { Fields = new[] { "proposer", "producer" }, Eq = delegat.Id }, null, null, level, timestamp, sort, offset, limit, quote)
                        : Task.FromResult(Enumerable.Empty<BakingOperation>());

                    var endorsingRewards = delegat.EndorsingRewardsCount > 0 && types.Contains(OpTypes.EndorsingReward)
                        ? Operations.GetEndorsingRewards(_delegat, level, timestamp, sort, offset, limit, quote)
                        : Task.FromResult(Enumerable.Empty<EndorsingRewardOperation>());

                    await Task.WhenAll(
                        endorsements,
                        preendorsements,
                        proposals,
                        ballots,
                        activations,
                        doubleBaking,
                        doubleEndorsing,
                        doublePreendorsing,
                        nonceRevelations,
                        vdfRevelations,
                        delegations,
                        originations,
                        transactions,
                        reveals,
                        registerConstants,
                        setDepositsLimits,
                        transferTicketOps,
                        txRollupCommitOps,
                        txRollupDispatchTicketsOps,
                        txRollupFinalizeCommitmentOps,
                        txRollupOriginationOps,
                        txRollupRejectionOps,
                        txRollupRemoveCommitmentOps,
                        txRollupReturnBondOps,
                        txRollupSubmitBatchOps,
                        increasePaidStorageOps,
                        migrations,
                        revelationPenalties,
                        bakingOps,
                        endorsingRewards);

                    result.AddRange(endorsements.Result);
                    result.AddRange(preendorsements.Result);
                    result.AddRange(proposals.Result);
                    result.AddRange(ballots.Result);
                    result.AddRange(activations.Result);
                    result.AddRange(doubleBaking.Result);
                    result.AddRange(doubleEndorsing.Result);
                    result.AddRange(doublePreendorsing.Result);
                    result.AddRange(nonceRevelations.Result);
                    result.AddRange(vdfRevelations.Result);
                    result.AddRange(delegations.Result);
                    result.AddRange(originations.Result);
                    result.AddRange(transactions.Result);
                    result.AddRange(reveals.Result);
                    result.AddRange(registerConstants.Result);
                    result.AddRange(setDepositsLimits.Result);
                    result.AddRange(transferTicketOps.Result);
                    result.AddRange(txRollupCommitOps.Result);
                    result.AddRange(txRollupDispatchTicketsOps.Result);
                    result.AddRange(txRollupFinalizeCommitmentOps.Result);
                    result.AddRange(txRollupOriginationOps.Result);
                    result.AddRange(txRollupRejectionOps.Result);
                    result.AddRange(txRollupRemoveCommitmentOps.Result);
                    result.AddRange(txRollupReturnBondOps.Result);
                    result.AddRange(txRollupSubmitBatchOps.Result);
                    result.AddRange(increasePaidStorageOps.Result);
                    result.AddRange(migrations.Result);
                    result.AddRange(revelationPenalties.Result);
                    result.AddRange(bakingOps.Result);
                    result.AddRange(endorsingRewards.Result);

                    break;
                case RawUser user:
                    var _user = new AccountParameter { Eq = user.Id };

                    var userActivations = user.Activated == true && types.Contains(OpTypes.Activation)
                        ? Operations.GetActivations(_user, level, timestamp, sort, offset, limit, quote)
                        : Task.FromResult(Enumerable.Empty<ActivationOperation>());

                    var userDelegations = user.DelegationsCount > 0 && types.Contains(OpTypes.Delegation)
                        ? Operations.GetDelegations(new AnyOfParameter { Fields = new[] { "initiator", "sender", "prevDelegate", "newDelegate" }, Eq = user.Id }, initiator, sender, prevDelegate, newDelegate, level, timestamp, null, status, sort, offset, limit, quote)
                        : Task.FromResult(Enumerable.Empty<DelegationOperation>());

                    var userOriginations = user.OriginationsCount > 0 && types.Contains(OpTypes.Origination)
                        ? Operations.GetOriginations(new AnyOfParameter { Fields = new[] { "initiator", "sender", "contractManager", "contractDelegate", "originatedContract" }, Eq = user.Id }, initiator, sender, contractManager, contractDelegate, originatedContract, null, null, null, level, timestamp, null, null, status, sort, offset, limit, format, quote)
                        : Task.FromResult(Enumerable.Empty<OriginationOperation>());

                    var userTransactions = user.TransactionsCount > 0 && types.Contains(OpTypes.Transaction)
                        ? Operations.GetTransactions(new AnyOfParameter { Fields = new[] { "initiator", "sender", "target" }, Eq = user.Id }, initiator, sender, target, null, null, level, timestamp, null, null, null, entrypoint, parameter, hasInternals, status, sort, offset, limit, format, quote)
                        : Task.FromResult(Enumerable.Empty<TransactionOperation>());

                    var userReveals = user.RevealsCount > 0 && types.Contains(OpTypes.Reveal)
                        ? Operations.GetReveals(_user, level, timestamp, status, sort, offset, limit, quote)
                        : Task.FromResult(Enumerable.Empty<RevealOperation>());

                    var userRegisterConstants = user.RegisterConstantsCount > 0 && types.Contains(OpTypes.RegisterConstant)
                        ? Operations.GetRegisterConstants(_user, null, level, timestamp, status, sort, offset, limit, format, quote)
                        : Task.FromResult(Enumerable.Empty<RegisterConstantOperation>());

                    var userSetDepositsLimit = user.SetDepositsLimitsCount > 0 && types.Contains(OpTypes.SetDepositsLimit)
                        ? Operations.GetSetDepositsLimits(_user, level, timestamp, status, sort, offset, limit, quote)
                        : Task.FromResult(Enumerable.Empty<SetDepositsLimitOperation>());

                    var userTransferTicketOps = user.TransferTicketCount > 0 && types.Contains(OpTypes.TransferTicket)
                        ? Operations.GetTransferTicketOps(null, _user, null, null, level, timestamp, status, sort, offset, limit, format, quote)
                        : Task.FromResult(Enumerable.Empty<TransferTicketOperation>());
                    
                    var userTxRollupCommitOps = user.TxRollupCommitCount > 0 && types.Contains(OpTypes.TxRollupCommit)
                        ? Operations.GetTxRollupCommitOps(_user, null, level, timestamp, status, sort, offset, limit, quote)
                        : Task.FromResult(Enumerable.Empty<TxRollupCommitOperation>());
                    
                    var userTxRollupDispatchTicketsOps = user.TxRollupDispatchTicketsCount > 0 && types.Contains(OpTypes.TxRollupDispatchTickets)
                        ? Operations.GetTxRollupDispatchTicketsOps(_user, null, level, timestamp, status, sort, offset, limit, quote)
                        : Task.FromResult(Enumerable.Empty<TxRollupDispatchTicketsOperation>());

                    var userTxRollupFinalizeCommitmentOps = user.TxRollupFinalizeCommitmentCount > 0 && types.Contains(OpTypes.TxRollupFinalizeCommitment)
                        ? Operations.GetTxRollupFinalizeCommitmentOps(_user, null, level, timestamp, status, sort, offset, limit, quote)
                        : Task.FromResult(Enumerable.Empty<TxRollupFinalizeCommitmentOperation>());

                    var userTxRollupOriginationOps = user.TxRollupOriginationCount > 0 && types.Contains(OpTypes.TxRollupOrigination)
                        ? Operations.GetTxRollupOriginationOps(_user, null, level, timestamp, status, sort, offset, limit, quote)
                        : Task.FromResult(Enumerable.Empty<TxRollupOriginationOperation>());

                    var userTxRollupRejectionOps = user.TxRollupRejectionCount > 0 && types.Contains(OpTypes.TxRollupRejection)
                        ? Operations.GetTxRollupRejectionOps(new AnyOfParameter { Fields = new[] { "sender", "committer" }, Eq = user.Id }, null, null, null, level, timestamp, status, sort, offset, limit, quote)
                        : Task.FromResult(Enumerable.Empty<TxRollupRejectionOperation>());

                    var userTxRollupRemoveCommitmentOps = user.TxRollupRemoveCommitmentCount > 0 && types.Contains(OpTypes.TxRollupRemoveCommitment)
                        ? Operations.GetTxRollupRemoveCommitmentOps(_user, null, level, timestamp, status, sort, offset, limit, quote)
                        : Task.FromResult(Enumerable.Empty<TxRollupRemoveCommitmentOperation>());

                    var userTxRollupReturnBondOps = user.TxRollupReturnBondCount > 0 && types.Contains(OpTypes.TxRollupReturnBond)
                        ? Operations.GetTxRollupReturnBondOps(_user, null, level, timestamp, status, sort, offset, limit, quote)
                        : Task.FromResult(Enumerable.Empty<TxRollupReturnBondOperation>());

                    var userTxRollupSubmitBatchOps = user.TxRollupSubmitBatchCount > 0 && types.Contains(OpTypes.TxRollupSubmitBatch)
                        ? Operations.GetTxRollupSubmitBatchOps(_user, null, level, timestamp, status, sort, offset, limit, quote)
                        : Task.FromResult(Enumerable.Empty<TxRollupSubmitBatchOperation>());

                    var userIncreasePaidStorageOps = user.IncreasePaidStorageCount > 0 && types.Contains(OpTypes.IncreasePaidStorage)
                        ? Operations.GetIncreasePaidStorageOps(_user, null, level, timestamp, status, sort, offset, limit, quote)
                        : Task.FromResult(Enumerable.Empty<IncreasePaidStorageOperation>());

                    var userMigrations = user.MigrationsCount > 0 && types.Contains(OpTypes.Migration)
                        ? Operations.GetMigrations(_user, null, null, null, level, timestamp, sort, offset, limit, format, quote)
                        : Task.FromResult(Enumerable.Empty<MigrationOperation>());

                    await Task.WhenAll(
                        userActivations,
                        userDelegations,
                        userOriginations,
                        userTransactions,
                        userReveals,
                        userRegisterConstants,
                        userSetDepositsLimit,
                        userTransferTicketOps,
                        userTxRollupCommitOps,
                        userTxRollupDispatchTicketsOps,
                        userTxRollupFinalizeCommitmentOps,
                        userTxRollupOriginationOps,
                        userTxRollupRejectionOps,
                        userTxRollupRemoveCommitmentOps,
                        userTxRollupReturnBondOps,
                        userTxRollupSubmitBatchOps,
                        userIncreasePaidStorageOps,
                        userMigrations);

                    result.AddRange(userActivations.Result);
                    result.AddRange(userDelegations.Result);
                    result.AddRange(userOriginations.Result);
                    result.AddRange(userTransactions.Result);
                    result.AddRange(userReveals.Result);
                    result.AddRange(userRegisterConstants.Result);
                    result.AddRange(userSetDepositsLimit.Result);
                    result.AddRange(userTransferTicketOps.Result);
                    result.AddRange(userTxRollupCommitOps.Result);
                    result.AddRange(userTxRollupDispatchTicketsOps.Result);
                    result.AddRange(userTxRollupFinalizeCommitmentOps.Result);
                    result.AddRange(userTxRollupOriginationOps.Result);
                    result.AddRange(userTxRollupRejectionOps.Result);
                    result.AddRange(userTxRollupRemoveCommitmentOps.Result);
                    result.AddRange(userTxRollupReturnBondOps.Result);
                    result.AddRange(userTxRollupSubmitBatchOps.Result);
                    result.AddRange(userIncreasePaidStorageOps.Result);
                    result.AddRange(userMigrations.Result);

                    break;
                case RawContract contract:
                    var _contract = new AccountParameter { Eq = contract.Id };

                    var contractDelegations = contract.DelegationsCount > 0 && types.Contains(OpTypes.Delegation)
                        ? Operations.GetDelegations(new AnyOfParameter { Fields = new[] { "initiator", "sender", "prevDelegate", "newDelegate" }, Eq = contract.Id }, initiator, sender, prevDelegate, newDelegate, level, timestamp, null, status, sort, offset, limit, quote)
                        : Task.FromResult(Enumerable.Empty<DelegationOperation>());

                    var contractOriginations = contract.OriginationsCount > 0 && types.Contains(OpTypes.Origination)
                        ? Operations.GetOriginations(new AnyOfParameter { Fields = new[] { "initiator", "sender", "contractManager", "contractDelegate", "originatedContract" }, Eq = contract.Id }, initiator, sender, contractManager, contractDelegate, originatedContract, null, null, null, level, timestamp, null, null, status, sort, offset, limit, format, quote)
                        : Task.FromResult(Enumerable.Empty<OriginationOperation>());

                    var contractTransactions = contract.TransactionsCount > 0 && types.Contains(OpTypes.Transaction)
                        ? Operations.GetTransactions(new AnyOfParameter { Fields = new[] { "initiator", "sender", "target" }, Eq = contract.Id }, initiator, sender, target, null, null, level, timestamp, null, null, null, entrypoint, parameter, hasInternals, status, sort, offset, limit, format, quote)
                        : Task.FromResult(Enumerable.Empty<TransactionOperation>());

                    var contractReveals = contract.RevealsCount > 0 && types.Contains(OpTypes.Reveal)
                        ? Operations.GetReveals(_contract, level, timestamp, status, sort, offset, limit, quote)
                        : Task.FromResult(Enumerable.Empty<RevealOperation>());

                    var contractTransferTicketOps = contract.TransferTicketCount > 0 && types.Contains(OpTypes.TransferTicket)
                        ? Operations.GetTransferTicketOps(new AnyOfParameter { Fields = new[] { "target", "ticketer" }, Eq = contract.Id }, null, null, null, level, timestamp, status, sort, offset, limit, format, quote)
                        : Task.FromResult(Enumerable.Empty<TransferTicketOperation>());

                    var contractIncreasePaidStorageOps = contract.IncreasePaidStorageCount > 0 && types.Contains(OpTypes.IncreasePaidStorage)
                        ? Operations.GetIncreasePaidStorageOps(null, _contract, level, timestamp, status, sort, offset, limit, quote)
                        : Task.FromResult(Enumerable.Empty<IncreasePaidStorageOperation>());

                    var contractMigrations = contract.MigrationsCount > 0 && types.Contains(OpTypes.Migration)
                        ? Operations.GetMigrations(_contract, null, null, null, level, timestamp, sort, offset, limit, format, quote)
                        : Task.FromResult(Enumerable.Empty<MigrationOperation>());

                    await Task.WhenAll(
                        contractDelegations,
                        contractOriginations,
                        contractTransactions,
                        contractReveals,
                        contractTransferTicketOps,
                        contractIncreasePaidStorageOps,
                        contractMigrations);

                    result.AddRange(contractDelegations.Result);
                    result.AddRange(contractOriginations.Result);
                    result.AddRange(contractTransactions.Result);
                    result.AddRange(contractReveals.Result);
                    result.AddRange(contractTransferTicketOps.Result);
                    result.AddRange(contractIncreasePaidStorageOps.Result);
                    result.AddRange(contractMigrations.Result);

                    break;

                case RawRollup rollup:
                    var _rollup = new AccountParameter { Eq = rollup.Id };

                    var rollupTransactionOps = rollup.TransactionsCount> 0 && types.Contains(OpTypes.Transaction)
                        ? Operations.GetTransactions(null, null, null, _rollup, null, null, level, timestamp, null, null, null, entrypoint, parameter, hasInternals, status, sort, offset, limit, format, quote)
                        : Task.FromResult(Enumerable.Empty<TransactionOperation>());

                    var rollupTxRollupCommitOps = rollup.TxRollupCommitCount > 0 && types.Contains(OpTypes.TxRollupCommit)
                        ? Operations.GetTxRollupCommitOps(null, _rollup, level, timestamp, status, sort, offset, limit, quote)
                        : Task.FromResult(Enumerable.Empty<TxRollupCommitOperation>());

                    var rollupTxRollupDispatchTicketsOps = rollup.TxRollupDispatchTicketsCount > 0 && types.Contains(OpTypes.TxRollupDispatchTickets)
                        ? Operations.GetTxRollupDispatchTicketsOps(null, _rollup, level, timestamp, status, sort, offset, limit, quote)
                        : Task.FromResult(Enumerable.Empty<TxRollupDispatchTicketsOperation>());

                    var rollupTxRollupFinalizeCommitmentOps = rollup.TxRollupFinalizeCommitmentCount > 0 && types.Contains(OpTypes.TxRollupFinalizeCommitment)
                        ? Operations.GetTxRollupFinalizeCommitmentOps(null, _rollup, level, timestamp, status, sort, offset, limit, quote)
                        : Task.FromResult(Enumerable.Empty<TxRollupFinalizeCommitmentOperation>());

                    var rollupTxRollupOriginationOps = rollup.TxRollupOriginationCount > 0 && types.Contains(OpTypes.TxRollupOrigination)
                        ? Operations.GetTxRollupOriginationOps(null, _rollup, level, timestamp, status, sort, offset, limit, quote)
                        : Task.FromResult(Enumerable.Empty<TxRollupOriginationOperation>());

                    var rollupTxRollupRejectionOps = rollup.TxRollupRejectionCount > 0 && types.Contains(OpTypes.TxRollupRejection)
                        ? Operations.GetTxRollupRejectionOps(null, null, null, _rollup, level, timestamp, status, sort, offset, limit, quote)
                        : Task.FromResult(Enumerable.Empty<TxRollupRejectionOperation>());

                    var rollupTxRollupRemoveCommitmentOps = rollup.TxRollupRemoveCommitmentCount > 0 && types.Contains(OpTypes.TxRollupRemoveCommitment)
                        ? Operations.GetTxRollupRemoveCommitmentOps(null, _rollup, level, timestamp, status, sort, offset, limit, quote)
                        : Task.FromResult(Enumerable.Empty<TxRollupRemoveCommitmentOperation>());

                    var rollupTxRollupReturnBondOps = rollup.TxRollupReturnBondCount > 0 && types.Contains(OpTypes.TxRollupReturnBond)
                        ? Operations.GetTxRollupReturnBondOps(null, _rollup, level, timestamp, status, sort, offset, limit, quote)
                        : Task.FromResult(Enumerable.Empty<TxRollupReturnBondOperation>());

                    var rollupTxRollupSubmitBatchOps = rollup.TxRollupSubmitBatchCount > 0 && types.Contains(OpTypes.TxRollupSubmitBatch)
                        ? Operations.GetTxRollupSubmitBatchOps(null, _rollup, level, timestamp, status, sort, offset, limit, quote)
                        : Task.FromResult(Enumerable.Empty<TxRollupSubmitBatchOperation>());

                    await Task.WhenAll(
                        rollupTransactionOps,
                        rollupTxRollupCommitOps,
                        rollupTxRollupDispatchTicketsOps,
                        rollupTxRollupFinalizeCommitmentOps,
                        rollupTxRollupOriginationOps,
                        rollupTxRollupRejectionOps,
                        rollupTxRollupRemoveCommitmentOps,
                        rollupTxRollupReturnBondOps,
                        rollupTxRollupSubmitBatchOps);

                    result.AddRange(rollupTransactionOps.Result);
                    result.AddRange(rollupTxRollupCommitOps.Result);
                    result.AddRange(rollupTxRollupDispatchTicketsOps.Result);
                    result.AddRange(rollupTxRollupFinalizeCommitmentOps.Result);
                    result.AddRange(rollupTxRollupOriginationOps.Result);
                    result.AddRange(rollupTxRollupRejectionOps.Result);
                    result.AddRange(rollupTxRollupRemoveCommitmentOps.Result);
                    result.AddRange(rollupTxRollupReturnBondOps.Result);
                    result.AddRange(rollupTxRollupSubmitBatchOps.Result);

                    break;

                case RawAccount ghost:
                    break;
            }

            return sort?.Desc == null
                ? result.OrderBy(x => x.Id).Take(limit)
                : result.OrderByDescending(x => x.Id).Take(limit);
        }

        public async Task<ProfileMetadata> GetMetadata(string address)
        {
            var account = await Accounts.GetAsync(address);
            return account?.Metadata;
        }
    }
}
