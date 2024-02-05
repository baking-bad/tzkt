using System.Data;
using Dapper;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Repositories
{
    public partial class AccountRepository : DbConnection
    {
        public async Task<Models.Delegate> GetDelegate(string address)
        {
            var rawAccount = await Accounts.GetAsync(address);
            if (rawAccount is not RawDelegate delegat)
                return null;

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
                StakedBalance = delegat.StakedBalance,
                StakedPseudotokens = delegat.StakedPseudotokens,
                UnstakedBalance = delegat.UnstakedBalance,
                UnstakedBaker = delegat.UnstakedBakerId == null ? null : Accounts.GetAlias(delegat.UnstakedBakerId.Value),
                TotalStakedBalance = delegat.TotalStakedBalance,
                ExternalStakedBalance = delegat.ExternalStakedBalance,
                ExternalUnstakedBalance = delegat.ExternalUnstakedBalance,
                IssuedPseudotokens = delegat.IssuedPseudotokens,
                StakersCount = delegat.StakersCount,
                LostBalance = delegat.LostBalance,
                TransferTicketCount = delegat.TransferTicketCount,
                TxRollupCommitCount = delegat.TxRollupCommitCount,
                TxRollupDispatchTicketsCount = delegat.TxRollupDispatchTicketsCount,
                TxRollupFinalizeCommitmentCount = delegat.TxRollupFinalizeCommitmentCount,
                TxRollupOriginationCount = delegat.TxRollupOriginationCount,
                TxRollupRejectionCount = delegat.TxRollupRejectionCount,
                TxRollupRemoveCommitmentCount = delegat.TxRollupRemoveCommitmentCount,
                TxRollupReturnBondCount = delegat.TxRollupReturnBondCount,
                TxRollupSubmitBatchCount = delegat.TxRollupSubmitBatchCount,
                VdfRevelationsCount = delegat.VdfRevelationsCount,
                IncreasePaidStorageCount = delegat.IncreasePaidStorageCount,
                UpdateConsensusKeyCount = delegat.UpdateConsensusKeyCount,
                DrainDelegateCount = delegat.DrainDelegateCount,
                FrozenDepositLimit = delegat.FrozenDepositLimit,
                LimitOfStakingOverBaking = delegat.LimitOfStakingOverBaking,
                EdgeOfBakingOverStaking = delegat.EdgeOfBakingOverStaking,
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
                ActiveTicketsCount = delegat.ActiveTicketsCount,
                TicketBalancesCount = delegat.TicketBalancesCount,
                TicketTransfersCount = delegat.TicketTransfersCount,
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
                SmartRollupBonds = delegat.SmartRollupBonds,
                SmartRollupsCount = delegat.SmartRollupsCount,
                SmartRollupAddMessagesCount = delegat.SmartRollupAddMessagesCount,
                SmartRollupCementCount = delegat.SmartRollupCementCount,
                SmartRollupExecuteCount = delegat.SmartRollupExecuteCount,
                SmartRollupOriginateCount = delegat.SmartRollupOriginateCount,
                SmartRollupPublishCount = delegat.SmartRollupPublishCount,
                SmartRollupRecoverBondCount = delegat.SmartRollupRecoverBondCount,
                SmartRollupRefuteCount = delegat.SmartRollupRefuteCount,
                RefutationGamesCount = delegat.RefutationGamesCount,
                ActiveRefutationGamesCount = delegat.ActiveRefutationGamesCount,
                StakingOpsCount = delegat.StakingOpsCount,
                AutostakingOpsCount = delegat.AutostakingOpsCount,
                Software = delegat.SoftwareId == null ? null : Software[(int)delegat.SoftwareId]
            };
        }

        public async Task<int> GetDelegatesCount(BoolParameter active)
        {
            var sql = new SqlBuilder(@"SELECT COUNT(*) FROM ""Accounts""")
                .Filter("Type", 1)
                .Filter("Staked", active);

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<Models.Delegate>> GetDelegates(
            BoolParameter active,
            Int32Parameter lastActivity,
            SortParameter sort,
            OffsetParameter offset,
            int limit)
        {
            var sql = new SqlBuilder($@"SELECT *, {AliasQuery} FROM ""Accounts""")
                .Filter("Type", 1)
                .Filter("Staked", active)
                .Filter("LastLevel", lastActivity)
                .Take(sort, offset, limit, x => x switch
                {
                    "activationLevel" => ("ActivationLevel", "ActivationLevel"),
                    "deactivationLevel" => ("DeactivationLevel", "DeactivationLevel"),
                    "stakingBalance" => ("StakingBalance", "StakingBalance"),
                    "balance" => ("Balance", "Balance"),
                    "numDelegators" => ("DelegatorsCount", "DelegatorsCount"),
                    _ => ("Id", "Id")
                });

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row =>
            {
                return new Models.Delegate
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
                    StakedBalance = row.StakedBalance,
                    StakedPseudotokens = row.StakedPseudotokens,
                    UnstakedBalance = row.UnstakedBalance,
                    UnstakedBaker = row.UnstakedBakerId == null ? null : Accounts.GetAlias(row.UnstakedBakerId.Value),
                    TotalStakedBalance = row.TotalStakedBalance,
                    ExternalStakedBalance = row.ExternalStakedBalance,
                    ExternalUnstakedBalance = row.ExternalUnstakedBalance,
                    IssuedPseudotokens = row.IssuedPseudotokens,
                    StakersCount = row.StakersCount,
                    LostBalance = row.LostBalance,
                    TransferTicketCount = row.TransferTicketCount,
                    TxRollupCommitCount = row.TxRollupCommitCount,
                    TxRollupDispatchTicketsCount = row.TxRollupDispatchTicketsCount,
                    TxRollupFinalizeCommitmentCount = row.TxRollupFinalizeCommitmentCount,
                    TxRollupOriginationCount = row.TxRollupOriginationCount,
                    TxRollupRejectionCount = row.TxRollupRejectionCount,
                    TxRollupRemoveCommitmentCount = row.TxRollupRemoveCommitmentCount,
                    TxRollupReturnBondCount = row.TxRollupReturnBondCount,
                    TxRollupSubmitBatchCount = row.TxRollupSubmitBatchCount,
                    VdfRevelationsCount = row.VdfRevelationsCount,
                    IncreasePaidStorageCount = row.IncreasePaidStorageCount,
                    UpdateConsensusKeyCount = row.UpdateConsensusKeyCount,
                    DrainDelegateCount = row.DrainDelegateCount,
                    FrozenDepositLimit = row.FrozenDepositLimit,
                    LimitOfStakingOverBaking = row.LimitOfStakingOverBaking,
                    EdgeOfBakingOverStaking = row.EdgeOfBakingOverStaking,
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
                    ActiveTicketsCount = row.ActiveTicketsCount,
                    TicketBalancesCount = row.TicketBalancesCount,
                    TicketTransfersCount = row.TicketTransfersCount,
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
                    SmartRollupBonds = row.SmartRollupBonds,
                    SmartRollupsCount = row.SmartRollupsCount,
                    SmartRollupAddMessagesCount = row.SmartRollupAddMessagesCount,
                    SmartRollupCementCount = row.SmartRollupCementCount,
                    SmartRollupExecuteCount = row.SmartRollupExecuteCount,
                    SmartRollupOriginateCount = row.SmartRollupOriginateCount,
                    SmartRollupPublishCount = row.SmartRollupPublishCount,
                    SmartRollupRecoverBondCount = row.SmartRollupRecoverBondCount,
                    SmartRollupRefuteCount = row.SmartRollupRefuteCount,
                    RefutationGamesCount = row.RefutationGamesCount,
                    ActiveRefutationGamesCount = row.ActiveRefutationGamesCount,
                    StakingOpsCount = row.StakingOpsCount,
                    AutostakingOpsCount = row.AutostakingOpsCount,
                    Software = row.SoftwareId == null ? null : Software[row.SoftwareId]
                };
            });
        }

        public async Task<object[][]> GetDelegates(
            BoolParameter active,
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
                    case "frozenDeposit": columns.Add(@"""TotalStakedBalance"""); break;
                    case "frozenDepositLimit": columns.Add(@"""FrozenDepositLimit"""); break;
                    case "limitOfStakingOverBaking": columns.Add(@"""LimitOfStakingOverBaking"""); break;
                    case "edgeOfBakingOverStaking": columns.Add(@"""EdgeOfBakingOverStaking"""); break;
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
                    case "activeTicketsCount": columns.Add(@"""ActiveTicketsCount"""); break;
                    case "ticketBalancesCount": columns.Add(@"""TicketBalancesCount"""); break;
                    case "ticketTransfersCount": columns.Add(@"""TicketTransfersCount"""); break;
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
                    case "stakedBalance": columns.Add(@"""StakedBalance"""); break;
                    case "stakedPseudotokens": columns.Add(@"""StakedPseudotokens"""); break;
                    case "unstakedBalance": columns.Add(@"""UnstakedBalance"""); break;
                    case "unstakedBaker": columns.Add(@"""UnstakedBakerId"""); break;
                    case "totalStakedBalance": columns.Add(@"""TotalStakedBalance"""); break;
                    case "externalStakedBalance": columns.Add(@"""ExternalStakedBalance"""); break;
                    case "externalUnstakedBalance": columns.Add(@"""ExternalUnstakedBalance"""); break;
                    case "issuedPseudotokens": columns.Add(@"""IssuedPseudotokens"""); break;
                    case "stakersCount": columns.Add(@"""StakersCount"""); break;
                    case "lostBalance": columns.Add(@"""LostBalance"""); break;
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
                    case "updateConsensusKeyCount": columns.Add(@"""UpdateConsensusKeyCount"""); break;
                    case "drainDelegateCount": columns.Add(@"""DrainDelegateCount"""); break;
                    case "smartRollupBonds": columns.Add(@"""SmartRollupBonds"""); break;
                    case "smartRollupsCount": columns.Add(@"""SmartRollupsCount"""); break;
                    case "smartRollupAddMessagesCount": columns.Add(@"""SmartRollupAddMessagesCount"""); break;
                    case "smartRollupCementCount": columns.Add(@"""SmartRollupCementCount"""); break;
                    case "smartRollupExecuteCount": columns.Add(@"""SmartRollupExecuteCount"""); break;
                    case "smartRollupOriginateCount": columns.Add(@"""SmartRollupOriginateCount"""); break;
                    case "smartRollupPublishCount": columns.Add(@"""SmartRollupPublishCount"""); break;
                    case "smartRollupRecoverBondCount": columns.Add(@"""SmartRollupRecoverBondCount"""); break;
                    case "smartRollupRefuteCount": columns.Add(@"""SmartRollupRefuteCount"""); break;
                    case "refutationGamesCount": columns.Add(@"""RefutationGamesCount"""); break;
                    case "activeRefutationGamesCount": columns.Add(@"""ActiveRefutationGamesCount"""); break;
                    case "stakingOpsCount": columns.Add(@"""StakingOpsCount"""); break;
                    case "autostakingOpsCount": columns.Add(@"""AutostakingOpsCount"""); break;
                }
            }

            if (columns.Count == 0)
                return Array.Empty<object[]>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""Accounts""")
                .Filter("Type", 1)
                .Filter("Staked", active)
                .Filter("LastLevel", lastActivity)
                .Take(sort, offset, limit, x => x switch
                {
                    "activationLevel" => ("ActivationLevel", "ActivationLevel"),
                    "deactivationLevel" => ("DeactivationLevel", "DeactivationLevel"),
                    "stakingBalance" => ("StakingBalance", "StakingBalance"),
                    "balance" => ("Balance", "Balance"),
                    "numDelegators" => ("DelegatorsCount", "DelegatorsCount"),
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
                            result[j++][i] = row.TotalStakedBalance;
                        break;
                    case "frozenDepositLimit":
                        foreach (var row in rows)
                            result[j++][i] = row.FrozenDepositLimit;
                        break;
                    case "limitOfStakingOverBaking":
                        foreach (var row in rows)
                            result[j++][i] = row.LimitOfStakingOverBaking;
                        break;
                    case "edgeOfBakingOverStaking":
                        foreach (var row in rows)
                            result[j++][i] = row.EdgeOfBakingOverStaking;
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
                            result[j++][i] = Time[row.ActivationLevel];
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
                    case "activeTicketsCount":
                        foreach (var row in rows)
                            result[j++][i] = row.ActiveTicketsCount;
                        break;
                    case "ticketBalancesCount":
                        foreach (var row in rows)
                            result[j++][i] = row.TicketBalancesCount;
                        break;
                    case "ticketTransfersCount":
                        foreach (var row in rows)
                            result[j++][i] = row.TicketTransfersCount;
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
                    case "stakedBalance":
                        foreach (var row in rows)
                            result[j++][i] = row.StakedBalance;
                        break;
                    case "stakedPseudotokens":
                        foreach (var row in rows)
                            result[j++][i] = row.StakedPseudotokens;
                        break;
                    case "unstakedBalance":
                        foreach (var row in rows)
                            result[j++][i] = row.UnstakedBalance;
                        break;
                    case "unstakedBaker":
                        foreach (var row in rows)
                            result[j++][i] = row.UnstakedBakerId == null ? null : Accounts.GetAlias(row.UnstakedBakerId);
                        break;
                    case "totalStakedBalance":
                        foreach (var row in rows)
                            result[j++][i] = row.TotalStakedBalance;
                        break;
                    case "externalStakedBalance":
                        foreach (var row in rows)
                            result[j++][i] = row.ExternalStakedBalance;
                        break;
                    case "externalUnstakedBalance":
                        foreach (var row in rows)
                            result[j++][i] = row.ExternalUnstakedBalance;
                        break;
                    case "issuedPseudotokens":
                        foreach (var row in rows)
                            result[j++][i] = row.IssuedPseudotokens;
                        break;
                    case "stakersCount":
                        foreach (var row in rows)
                            result[j++][i] = row.StakersCount;
                        break;
                    case "lostBalance":
                        foreach (var row in rows)
                            result[j++][i] = row.LostBalance;
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
                    case "updateConsensusKeyCount":
                        foreach (var row in rows)
                            result[j++][i] = row.UpdateConsensusKeyCount;
                        break;
                    case "drainDelegateCount":
                        foreach (var row in rows)
                            result[j++][i] = row.DrainDelegateCount;
                        break;
                    case "smartRollupBonds":
                        foreach (var row in rows)
                            result[j++][i] = row.SmartRollupBonds;
                        break;
                    case "smartRollupsCount":
                        foreach (var row in rows)
                            result[j++][i] = row.SmartRollupsCount;
                        break;
                    case "smartRollupAddMessagesCount":
                        foreach (var row in rows)
                            result[j++][i] = row.SmartRollupAddMessagesCount;
                        break;
                    case "smartRollupCementCount":
                        foreach (var row in rows)
                            result[j++][i] = row.SmartRollupCementCount;
                        break;
                    case "smartRollupExecuteCount":
                        foreach (var row in rows)
                            result[j++][i] = row.SmartRollupExecuteCount;
                        break;
                    case "smartRollupOriginateCount":
                        foreach (var row in rows)
                            result[j++][i] = row.SmartRollupOriginateCount;
                        break;
                    case "smartRollupPublishCount":
                        foreach (var row in rows)
                            result[j++][i] = row.SmartRollupPublishCount;
                        break;
                    case "smartRollupRecoverBondCount":
                        foreach (var row in rows)
                            result[j++][i] = row.SmartRollupRecoverBondCount;
                        break;
                    case "smartRollupRefuteCount":
                        foreach (var row in rows)
                            result[j++][i] = row.SmartRollupRefuteCount;
                        break;
                    case "refutationGamesCount":
                        foreach (var row in rows)
                            result[j++][i] = row.RefutationGamesCount;
                        break;
                    case "activeRefutationGamesCount":
                        foreach (var row in rows)
                            result[j++][i] = row.ActiveRefutationGamesCount;
                        break;
                    case "stakingOpsCount":
                        foreach (var row in rows)
                            result[j++][i] = row.StakingOpsCount;
                        break;
                    case "autostakingOpsCount":
                        foreach (var row in rows)
                            result[j++][i] = row.AutostakingOpsCount;
                        break;
                }
            }
            
            return result;
        }

        public async Task<object[]> GetDelegates(
            BoolParameter active,
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
                case "frozenDeposit": columns.Add(@"""TotalStakedBalance"""); break;
                case "frozenDepositLimit": columns.Add(@"""FrozenDepositLimit"""); break;
                case "limitOfStakingOverBaking": columns.Add(@"""LimitOfStakingOverBaking"""); break;
                case "edgeOfBakingOverStaking": columns.Add(@"""EdgeOfBakingOverStaking"""); break;
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
                case "activeTicketsCount": columns.Add(@"""ActiveTicketsCount"""); break;
                case "ticketBalancesCount": columns.Add(@"""TicketBalancesCount"""); break;
                case "ticketTransfersCount": columns.Add(@"""TicketTransfersCount"""); break;
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
                case "stakedBalance": columns.Add(@"""StakedBalance"""); break;
                case "stakedPseudotokens": columns.Add(@"""StakedPseudotokens"""); break;
                case "unstakedBalance": columns.Add(@"""UnstakedBalance"""); break;
                case "unstakedBaker": columns.Add(@"""UnstakedBakerId"""); break;
                case "totalStakedBalance": columns.Add(@"""TotalStakedBalance"""); break;
                case "externalStakedBalance": columns.Add(@"""ExternalStakedBalance"""); break;
                case "externalUnstakedBalance": columns.Add(@"""ExternalUnstakedBalance"""); break;
                case "issuedPseudotokens": columns.Add(@"""IssuedPseudotokens"""); break;
                case "stakersCount": columns.Add(@"""StakersCount"""); break;
                case "lostBalance": columns.Add(@"""LostBalance"""); break;
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
                case "updateConsensusKeyCount": columns.Add(@"""UpdateConsensusKeyCount"""); break;
                case "drainDelegateCount": columns.Add(@"""DrainDelegateCount"""); break;
                case "smartRollupBonds": columns.Add(@"""SmartRollupBonds"""); break;
                case "smartRollupsCount": columns.Add(@"""SmartRollupsCount"""); break;
                case "smartRollupAddMessagesCount": columns.Add(@"""SmartRollupAddMessagesCount"""); break;
                case "smartRollupCementCount": columns.Add(@"""SmartRollupCementCount"""); break;
                case "smartRollupExecuteCount": columns.Add(@"""SmartRollupExecuteCount"""); break;
                case "smartRollupOriginateCount": columns.Add(@"""SmartRollupOriginateCount"""); break;
                case "smartRollupPublishCount": columns.Add(@"""SmartRollupPublishCount"""); break;
                case "smartRollupRecoverBondCount": columns.Add(@"""SmartRollupRecoverBondCount"""); break;
                case "smartRollupRefuteCount": columns.Add(@"""SmartRollupRefuteCount"""); break;
                case "refutationGamesCount": columns.Add(@"""RefutationGamesCount"""); break;
                case "activeRefutationGamesCount": columns.Add(@"""ActiveRefutationGamesCount"""); break;
                case "stakingOpsCount": columns.Add(@"""StakingOpsCount"""); break;
                case "autostakingOpsCount": columns.Add(@"""AutostakingOpsCount"""); break;
            }

            if (columns.Count == 0)
                return Array.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""Accounts""")
                .Filter("Type", 1)
                .Filter("Staked", active)
                .Filter("LastLevel", lastActivity)
                .Take(sort, offset, limit, x => x switch
                {
                    "activationLevel" => ("ActivationLevel", "ActivationLevel"),
                    "deactivationLevel" => ("DeactivationLevel", "DeactivationLevel"),
                    "stakingBalance" => ("StakingBalance", "StakingBalance"),
                    "balance" => ("Balance", "Balance"),
                    "numDelegators" => ("DelegatorsCount", "DelegatorsCount"),
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
                        result[j++] = row.TotalStakedBalance;
                    break;
                case "frozenDepositLimit":
                    foreach (var row in rows)
                        result[j++] = row.FrozenDepositLimit;
                    break;
                case "limitOfStakingOverBaking":
                    foreach (var row in rows)
                        result[j++] = row.LimitOfStakingOverBaking;
                    break;
                case "edgeOfBakingOverStaking":
                    foreach (var row in rows)
                        result[j++] = row.EdgeOfBakingOverStaking;
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
                        result[j++] = Time[row.ActivationLevel];
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
                case "activeTicketsCount":
                    foreach (var row in rows)
                        result[j++] = row.ActiveTicketsCount;
                    break;
                case "ticketBalancesCount":
                    foreach (var row in rows)
                        result[j++] = row.TicketBalancesCount;
                    break;
                case "ticketTransfersCount":
                    foreach (var row in rows)
                        result[j++] = row.TicketTransfersCount;
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
                case "stakedBalance":
                    foreach (var row in rows)
                        result[j++] = row.StakedBalance;
                    break;
                case "stakedPseudotokens":
                    foreach (var row in rows)
                        result[j++] = row.StakedPseudotokens;
                    break;
                case "unstakedBalance":
                    foreach (var row in rows)
                        result[j++] = row.UnstakedBalance;
                    break;
                case "unstakedBaker":
                    foreach (var row in rows)
                        result[j++] = row.UnstakedBakerId == null ? null : Accounts.GetAlias(row.UnstakedBakerId);
                    break;
                case "totalStakedBalance":
                    foreach (var row in rows)
                        result[j++] = row.TotalStakedBalance;
                    break;
                case "externalStakedBalance":
                    foreach (var row in rows)
                        result[j++] = row.ExternalStakedBalance;
                    break;
                case "externalUnstakedBalance":
                    foreach (var row in rows)
                        result[j++] = row.ExternalUnstakedBalance;
                    break;
                case "issuedPseudotokens":
                    foreach (var row in rows)
                        result[j++] = row.IssuedPseudotokens;
                    break;
                case "stakersCount":
                    foreach (var row in rows)
                        result[j++] = row.StakersCount;
                    break;
                case "lostBalance":
                    foreach (var row in rows)
                        result[j++] = row.LostBalance;
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
                case "updateConsensusKeyCount":
                    foreach (var row in rows)
                        result[j++] = row.UpdateConsensusKeyCount;
                    break;
                case "drainDelegateCount":
                    foreach (var row in rows)
                        result[j++] = row.DrainDelegateCount;
                    break;
                case "smartRollupBonds":
                    foreach (var row in rows)
                        result[j++] = row.SmartRollupBonds;
                    break;
                case "smartRollupsCount":
                    foreach (var row in rows)
                        result[j++] = row.SmartRollupsCount;
                    break;
                case "smartRollupAddMessagesCount":
                    foreach (var row in rows)
                        result[j++] = row.SmartRollupAddMessagesCount;
                    break;
                case "smartRollupCementCount":
                    foreach (var row in rows)
                        result[j++] = row.SmartRollupCementCount;
                    break;
                case "smartRollupExecuteCount":
                    foreach (var row in rows)
                        result[j++] = row.SmartRollupExecuteCount;
                    break;
                case "smartRollupOriginateCount":
                    foreach (var row in rows)
                        result[j++] = row.SmartRollupOriginateCount;
                    break;
                case "smartRollupPublishCount":
                    foreach (var row in rows)
                        result[j++] = row.SmartRollupPublishCount;
                    break;
                case "smartRollupRecoverBondCount":
                    foreach (var row in rows)
                        result[j++] = row.SmartRollupRecoverBondCount;
                    break;
                case "smartRollupRefuteCount":
                    foreach (var row in rows)
                        result[j++] = row.SmartRollupRefuteCount;
                    break;
                case "refutationGamesCount":
                    foreach (var row in rows)
                        result[j++] = row.RefutationGamesCount;
                    break;
                case "activeRefutationGamesCount":
                    foreach (var row in rows)
                        result[j++] = row.ActiveRefutationGamesCount;
                    break;
                case "stakingOpsCount":
                    foreach (var row in rows)
                        result[j++] = row.StakingOpsCount;
                    break;
                case "autostakingOpsCount":
                    foreach (var row in rows)
                        result[j++] = row.AutostakingOpsCount;
                    break;
            }

            return result;
        }
    }
}
