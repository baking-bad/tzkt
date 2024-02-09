﻿using System.Text;
using Dapper;
using Mvkt.Api.Models;
using Mvkt.Api.Services.Cache;

namespace Mvkt.Api.Repositories
{
    public class BalanceHistoryRepository : DbConnection
    {
        readonly StateCache State;
        readonly AccountsCache Accounts;
        readonly QuotesCache Quotes;
        readonly TimeCache Time;

        public BalanceHistoryRepository(StateCache state, AccountsCache accounts, QuotesCache quotes, TimeCache time, IConfiguration config) : base(config)
        {
            State = state;
            Accounts = accounts;
            Quotes = quotes;
            Time = time;
        }

        public Task<long> Get(string address, DateTime timestamp)
        {
            var level = Time.FindLevel(timestamp, SearchMode.ExactOrLower);
            if (level == -1) return Task.FromResult(0L);
            return Get(address, level);
        }

        public async Task<long> Get(string address, int level)
        {
            var account = await Accounts.GetAsync(address);
            if (account == null || level < account.FirstLevel) return 0;
            if (level >= account.LastLevel) return account.Balance;

            var to = level - account.FirstLevel < account.LastLevel - level ? level : 0;
            var from = to == 0 ? level : 0;

            var union = SumUnion(account, from, to);
            if (union.Length == 0) return 0;

            var sql = from == 0 
                ? $@"
                    SELECT SUM(""Change"")::bigint
                    FROM ({union}) as u"
                : $@"
                    SELECT      acc.""Balance"" - COALESCE((SUM(""Change"") OVER ())::bigint, 0)
                    FROM        ({union}) as u
                    INNER JOIN  ""Accounts"" as acc
                            ON  acc.""Id"" = {account.Id}";

            using var db = GetConnection();
            return await db.ExecuteScalarAsync<long>(sql, new { account = account.Id, level });
        }

        public async Task<IEnumerable<HistoricalBalance>> Get(
            string address,
            int step,
            SortParameter sort,
            int offset,
            int limit,
            Symbols quote)
        {
            var account = await Accounts.GetAsync(address);
            if (account == null) return Enumerable.Empty<HistoricalBalance>();

            #region dumb users
            if (limit == 1 && offset == 0 && sort?.Desc != null)
            {
                return new[]
                {
                    new HistoricalBalance
                    {
                        Level = account.LastLevel,
                        Timestamp = Time[account.LastLevel],
                        Balance = account.Balance,
                        Quote = Quotes.Get(quote, account.LastLevel)
                    }
                };
            }
            #endregion

            var union = SelectUnion(account);
            if (union.Length == 0) return Enumerable.Empty<HistoricalBalance>();

            var key = step > 1
                ? @"(""Level"" + @step - 1) / @step * @step"
                : @"""Level""";

            var orderBy = sort?.Desc == "level" ? "ORDER BY lvl DESC" : "";

            var sql = $@"
                SELECT lvl as ""Level"", (SUM(""Change"") OVER (ORDER BY lvl asc))::bigint as ""Balance""
                FROM (
                    SELECT {key} as lvl, SUM(""Change"")::bigint as ""Change""
                    FROM ({union}) as u
                    GROUP BY lvl
                    ORDER BY lvl
                ) as gr
                WHERE lvl <= {State.Current.Level}
                {orderBy}
                OFFSET @offset
                LIMIT @limit";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { account = account.Id, step, offset, limit });

            return rows.Select(row => new HistoricalBalance
            {
                Level = row.Level,
                Timestamp = Time[row.Level],
                Balance = row.Balance,
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<object[][]> Get(
            string address,
            int step,
            SortParameter sort,
            int offset,
            int limit,
            string[] fields,
            Symbols quote)
        {
            var account = await Accounts.GetAsync(address);
            if (account == null) return Array.Empty<object[]>();

            var union = SelectUnion(account);
            if (union.Length == 0) return Array.Empty<object[]>();

            var key = step > 1
                ? @"(""Level"" + @step - 1) / @step * @step"
                : @"""Level""";

            var orderBy = sort?.Desc == "level" ? "ORDER BY lvl DESC" : "";

            var sql = $@"
                SELECT lvl as ""Level"", (SUM(""Change"") OVER (ORDER BY lvl asc))::bigint as ""Balance""
                FROM (
                    SELECT {key} as lvl, SUM(""Change"")::bigint as ""Change""
                    FROM ({union}) as u
                    GROUP BY lvl
                    ORDER BY lvl
                ) as gr
                WHERE lvl <= {State.Current.Level}
                {orderBy}
                OFFSET @offset
                LIMIT @limit";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { account = account.Id, step, offset, limit });

            var result = new object[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object[fields.Length];

            for (int i = 0, j = 0; i < fields.Length; j = 0, i++)
            {
                switch (fields[i])
                {
                    case "level":
                        foreach (var row in rows)
                            result[j++][i] = row.Level;
                        break;
                    case "timestamp":
                        foreach (var row in rows)
                            result[j++][i] = Time[row.Level];
                        break;
                    case "balance":
                        foreach (var row in rows)
                            result[j++][i] = row.Balance;
                        break;
                    case "quote":
                        foreach (var row in rows)
                            result[j++][i] = Quotes.Get(quote, row.Level);
                        break;
                }
            }

            return result;
        }

        public async Task<object[]> Get(
            string address,
            int step,
            SortParameter sort,
            int offset,
            int limit,
            string field,
            Symbols quote)
        {
            var account = await Accounts.GetAsync(address);
            if (account == null) return Array.Empty<object>();

            var union = SelectUnion(account);
            if (union.Length == 0) return Array.Empty<object>();

            var key = step > 1
                ? @"(""Level"" + @step - 1) / @step * @step"
                : @"""Level""";

            var orderBy = sort?.Desc == "level" ? "ORDER BY lvl DESC" : "";

            var sql = $@"
                SELECT lvl as ""Level"", (SUM(""Change"") OVER (ORDER BY lvl asc))::bigint as ""Balance""
                FROM (
                    SELECT {key} as lvl, SUM(""Change"")::bigint as ""Change""
                    FROM ({union}) as u
                    GROUP BY lvl
                    ORDER BY lvl
                ) as gr
                WHERE lvl <= {State.Current.Level}
                {orderBy}
                OFFSET @offset
                LIMIT @limit";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { account = account.Id, step, offset, limit });

            //TODO: optimize memory allocation
            var result = new object[rows.Count()];
            var j = 0;

            switch (field)
            {
                case "level":
                    foreach (var row in rows)
                        result[j++] = row.Level;
                    break;
                case "timestamp":
                    foreach (var row in rows)
                        result[j++] = Time[row.Level];
                    break;
                case "balance":
                    foreach (var row in rows)
                        result[j++] = row.Balance;
                    break;
                case "quote":
                    foreach (var row in rows)
                        result[j++] = Quotes.Get(quote, row.Level);
                    break;
            }

            return result;
        }

        #region sum union
        string SumUnion(RawAccount account, int from, int to)
        {
            var union = new StringBuilder();

            if (account.DelegationsCount > 0) SumDelegations(union, from, to);
            if (account.OriginationsCount > 0) SumOriginations(union, from, to);
            if (account.TransactionsCount > 0) SumTransactions(union, from, to);
            if (account.RevealsCount > 0) SumReveals(union, from, to);
            if (account.MigrationsCount > 0) SumMigrations(union, from, to);
            if (account.TxRollupCommitCount > 0) SumTxRollupCommitOps(union, from, to);
            if (account.TxRollupDispatchTicketsCount > 0) SumTxRollupDispatchTicketsOps(union, from, to);
            if (account.TxRollupFinalizeCommitmentCount > 0) SumTxRollupFinalizeCommitmentOps(union, from, to);
            if (account.TxRollupOriginationCount > 0) SumTxRollupOriginationOps(union, from, to);
            if (account.TxRollupRejectionCount > 0) SumTxRollupRejectionOps(union, from, to);
            if (account.TxRollupRemoveCommitmentCount > 0) SumTxRollupRemoveCommitmentOps(union, from, to);
            if (account.TxRollupReturnBondCount > 0) SumTxRollupReturnBondOps(union, from, to);
            if (account.TxRollupSubmitBatchCount > 0) SumTxRollupSubmitBatchOps(union, from, to);
            if (account.TransferTicketCount > 0) SumTransferTicketOps(union, from, to);
            if (account.IncreasePaidStorageCount > 0) SumIncreasePaidStorageOps(union, from, to);
            if (account.SmartRollupAddMessagesCount > 0) SumSrAddMessagesOps(union, from, to);
            if (account.SmartRollupCementCount > 0) SumSrCementOps(union, from, to);
            if (account.SmartRollupExecuteCount > 0) SumSrExecuteOps(union, from, to);
            if (account.SmartRollupOriginateCount > 0) SumSrOriginateOps(union, from, to);
            if (account.SmartRollupPublishCount > 0) SumSrPublishOps(union, from, to);
            if (account.SmartRollupRecoverBondCount > 0) SumSrRecoverBondOps(union, from, to);
            if (account.SmartRollupRefuteCount > 0) SumSrRefuteOps(union, from, to);
            if (account.RefutationGamesCount > 0) SumSrGames(union, from, to);

            if (account is RawUser user)
            {
                if (user.Activated == true) SumActivations(union, from, to);
                if (user.RegisterConstantsCount > 0) SumRegisterConstants(union, from, to);
                if (user.SetDepositsLimitsCount > 0) SumSetDepositsLimits(union, from, to);
                if (user.DrainDelegateCount > 0) SumDrainDelegateOps(union, from, to);
                if (user.StakingOpsCount > 0) SumStakingOps(union, from, to);
            }

            if (account is RawDelegate delegat)
            {
                if (delegat.EndorsingRewardsCount > 0) SumEndorsingRewards(union, from, to);
                if (delegat.BlocksCount > 0) SumBaking(union, from, to);
                if (delegat.EndorsementsCount > 0) SumEndorsements(union, from, to);
                if (delegat.DoubleBakingCount > 0) SumDoubleBaking(union, from, to);
                if (delegat.DoubleEndorsingCount > 0) SumDoubleEndorsing(union, from, to);
                if (delegat.DoublePreendorsingCount > 0) SumDoublePreendorsing(union, from, to);
                if (delegat.NonceRevelationsCount > 0) SumNonceRevelations(union, from, to);
                if (delegat.VdfRevelationsCount > 0) SumVdfRevelations(union, from, to);
                if (delegat.RevelationPenaltiesCount > 0) SumRevelationPenalties(union, from, to);
                if (delegat.UpdateConsensusKeyCount > 0) SumUpdateConsensusKeyOps(union, from, to);
            }

            return union.ToString();
        }

        void SumEndorsingRewards(StringBuilder sql, int from, int to)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"SUM(""RewardLiquid"" + ""RewardStakedOwn"") as ""Change"" ");
            sql.Append(@"FROM ""EndorsingRewardOps"" ");
            sql.Append(@"WHERE ""BakerId"" = @account ");
            sql.Append(@"AND (""RewardLiquid"" != 0 OR ""RewardStakedOwn"" != 0) ");

            if (from > 0)
                sql.Append($@"AND ""Level"" > {from} ");
            else if (to > 0)
                sql.Append($@"AND ""Level"" <= {to} ");

            sql.AppendLine();
        }

        void SumBaking(StringBuilder sql, int from, int to)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            #region proposer
            sql.Append(@"SUM(""RewardLiquid"" + ""RewardStakedOwn"" + ""Fees"") as ""Change"" ");
            sql.Append(@"FROM ""Blocks"" ");
            sql.Append(@"WHERE ""ProposerId"" = @account ");

            if (from > 0)
                sql.Append($@"AND ""Level"" > {from} ");
            else if (to > 0)
                sql.Append($@"AND ""Level"" <= {to} ");

            sql.AppendLine();
            #endregion

            sql.Append("UNION ALL SELECT ");

            #region producer
            sql.Append(@"SUM(""BonusLiquid"" + ""BonusStakedOwn"") as ""Change"" ");
            sql.Append(@"FROM ""Blocks"" ");
            sql.Append(@"WHERE ""ProducerId"" = @account ");

            if (from > 0)
                sql.Append($@"AND ""Level"" > {from} ");
            else if (to > 0)
                sql.Append($@"AND ""Level"" <= {to} ");

            sql.AppendLine();
            #endregion
        }

        void SumEndorsements(StringBuilder sql, int from, int to)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"SUM(""Reward"") as ""Change"" ");
            sql.Append(@"FROM ""EndorsementOps"" ");
            sql.Append(@"WHERE ""DelegateId"" = @account ");
            sql.Append(@"AND ""Reward"" != 0 ");

            if (from > 0)
                sql.Append($@"AND ""Level"" > {from} ");
            else if (to > 0)
                sql.Append($@"AND ""Level"" <= {to} ");

            sql.AppendLine();
        }

        void SumActivations(StringBuilder sql, int from, int to)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"SUM(""Balance"") as ""Change"" ");
            sql.Append(@"FROM ""ActivationOps"" ");
            sql.Append(@"WHERE ""AccountId"" = @account ");

            if (from > 0)
                sql.Append($@"AND ""Level"" > {from} ");
            else if (to > 0)
                sql.Append($@"AND ""Level"" <= {to} ");

            sql.AppendLine();
        }

        void SumDoubleBaking(StringBuilder sql, int from, int to)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            #region accuser
            sql.Append(@"SUM(""Reward"") as ""Change"" ");
            sql.Append(@"FROM ""DoubleBakingOps"" ");
            sql.Append(@"WHERE ""AccuserId"" = @account ");

            if (from > 0)
                sql.Append($@"AND ""Level"" > {from} ");
            else if (to > 0)
                sql.Append($@"AND ""Level"" <= {to} ");

            sql.AppendLine();
            #endregion

            sql.Append("UNION ALL SELECT ");

            #region offender
            sql.Append(@"SUM(-""LostStaked"" - ""LostUnstaked"") as ""Change"" ");
            sql.Append(@"FROM ""DoubleBakingOps"" ");
            sql.Append(@"WHERE ""OffenderId"" = @account ");

            if (from > 0)
                sql.Append($@"AND ""Level"" > {from} ");
            else if (to > 0)
                sql.Append($@"AND ""Level"" <= {to} ");

            sql.AppendLine();
            #endregion
        }

        void SumDoubleEndorsing(StringBuilder sql, int from, int to)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            #region accuser
            sql.Append(@"SUM(""Reward"") as ""Change"" ");
            sql.Append(@"FROM ""DoubleEndorsingOps"" ");
            sql.Append(@"WHERE ""AccuserId"" = @account ");

            if (from > 0)
                sql.Append($@"AND ""Level"" > {from} ");
            else if (to > 0)
                sql.Append($@"AND ""Level"" <= {to} ");

            sql.AppendLine();
            #endregion

            sql.Append("UNION ALL SELECT ");

            #region offender
            sql.Append(@"SUM(-""LostStaked"" - ""LostUnstaked"") as ""Change"" ");
            sql.Append(@"FROM ""DoubleEndorsingOps"" ");
            sql.Append(@"WHERE ""OffenderId"" = @account ");

            if (from > 0)
                sql.Append($@"AND ""Level"" > {from} ");
            else if (to > 0)
                sql.Append($@"AND ""Level"" <= {to} ");

            sql.AppendLine();
            #endregion
        }

        void SumDoublePreendorsing(StringBuilder sql, int from, int to)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            #region accuser
            sql.Append(@"SUM(""Reward"") as ""Change"" ");
            sql.Append(@"FROM ""DoublePreendorsingOps"" ");
            sql.Append(@"WHERE ""AccuserId"" = @account ");

            if (from > 0)
                sql.Append($@"AND ""Level"" > {from} ");
            else if (to > 0)
                sql.Append($@"AND ""Level"" <= {to} ");

            sql.AppendLine();
            #endregion

            sql.Append("UNION ALL SELECT ");

            #region offender
            sql.Append(@"SUM(-""LostStaked"" - ""LostUnstaked"") as ""Change"" ");
            sql.Append(@"FROM ""DoublePreendorsingOps"" ");
            sql.Append(@"WHERE ""OffenderId"" = @account ");

            if (from > 0)
                sql.Append($@"AND ""Level"" > {from} ");
            else if (to > 0)
                sql.Append($@"AND ""Level"" <= {to} ");

            sql.AppendLine();
            #endregion
        }

        void SumNonceRevelations(StringBuilder sql, int from, int to)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"SUM(""RewardLiquid"" + ""RewardStakedOwn"") as ""Change"" ");
            sql.Append(@"FROM ""NonceRevelationOps"" ");
            sql.Append(@"WHERE ""BakerId"" = @account ");

            if (from > 0)
                sql.Append($@"AND ""Level"" > {from} ");
            else if (to > 0)
                sql.Append($@"AND ""Level"" <= {to} ");

            sql.AppendLine();
        }

        void SumVdfRevelations(StringBuilder sql, int from, int to)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"SUM(""RewardLiquid"" + ""RewardStakedOwn"") as ""Change"" ");
            sql.Append(@"FROM ""VdfRevelationOps"" ");
            sql.Append(@"WHERE ""BakerId"" = @account ");

            if (from > 0)
                sql.Append($@"AND ""Level"" > {from} ");
            else if (to > 0)
                sql.Append($@"AND ""Level"" <= {to} ");

            sql.AppendLine();
        }

        void SumDelegations(StringBuilder sql, int from, int to)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"SUM(-""BakerFee"") as ""Change"" ");
            sql.Append(@"FROM ""DelegationOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");

            if (from > 0)
                sql.Append($@"AND ""Level"" > {from} ");
            else if (to > 0)
                sql.Append($@"AND ""Level"" <= {to} ");

            sql.AppendLine();
        }

        void SumOriginations(StringBuilder sql, int from, int to)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            #region originated
            sql.Append(@"SUM(""Balance"") as ""Change"" ");
            sql.Append(@"FROM ""OriginationOps"" ");
            sql.Append(@"WHERE ""ContractId"" = @account ");

            if (from > 0)
                sql.Append($@"AND ""Level"" > {from} ");
            else if (to > 0)
                sql.Append($@"AND ""Level"" <= {to} ");

            sql.AppendLine();
            #endregion

            sql.Append("UNION ALL SELECT ");

            #region sent
            sql.Append(@"SUM(-""Balance"" -(CASE WHEN ""Nonce"" is NULL THEN (""BakerFee"" + COALESCE(""StorageFee"", 0) + COALESCE(""AllocationFee"", 0)) ELSE 0 END)) as ""Change"" ");
            sql.Append(@"FROM ""OriginationOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");
            sql.Append(@"AND ""Status"" = 1 ");

            if (from > 0)
                sql.Append($@"AND ""Level"" > {from} ");
            else if (to > 0)
                sql.Append($@"AND ""Level"" <= {to} ");

            sql.AppendLine();
            #endregion

            sql.Append("UNION ALL SELECT ");

            #region internal
            sql.Append(@"SUM(-COALESCE(""StorageFee"", 0) - COALESCE(""AllocationFee"", 0)) as ""Change"" ");
            sql.Append(@"FROM ""OriginationOps"" ");
            sql.Append(@"WHERE ""InitiatorId"" = @account ");
            sql.Append(@"AND ""Status"" = 1 ");

            if (from > 0)
                sql.Append($@"AND ""Level"" > {from} ");
            else if (to > 0)
                sql.Append($@"AND ""Level"" <= {to} ");

            sql.AppendLine();
            #endregion

            sql.Append("UNION ALL SELECT ");

            #region failed
            sql.Append(@"SUM(-""BakerFee"") as ""Change"" ");
            sql.Append(@"FROM ""OriginationOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");
            sql.Append(@"AND ""Status"" != 1 ");

            if (from > 0)
                sql.Append($@"AND ""Level"" > {from} ");
            else if (to > 0)
                sql.Append($@"AND ""Level"" <= {to} ");

            sql.AppendLine();
            #endregion
        }

        void SumTransactions(StringBuilder sql, int from, int to)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            #region received
            sql.Append(@"SUM(""Amount"") as ""Change"" ");
            sql.Append(@"FROM ""TransactionOps"" ");
            sql.Append(@"WHERE ""TargetId"" = @account ");
            sql.Append(@"AND ""Status"" = 1 ");

            if (from > 0)
                sql.Append($@"AND ""Level"" > {from} ");
            else if (to > 0)
                sql.Append($@"AND ""Level"" <= {to} ");

            sql.AppendLine();
            #endregion

            sql.Append("UNION ALL SELECT ");

            #region sent
            sql.Append(@"SUM(-""Amount"" - (CASE WHEN ""Nonce"" is NULL THEN (""BakerFee"" + COALESCE(""StorageFee"", 0) + COALESCE(""AllocationFee"", 0)) ELSE 0 END)) as ""Change"" ");
            sql.Append(@"FROM ""TransactionOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");
            sql.Append(@"AND ""Status"" = 1 ");

            if (from > 0)
                sql.Append($@"AND ""Level"" > {from} ");
            else if (to > 0)
                sql.Append($@"AND ""Level"" <= {to} ");

            sql.AppendLine();
            #endregion

            sql.Append("UNION ALL SELECT ");

            #region internal
            sql.Append(@"SUM(-COALESCE(""StorageFee"", 0) - COALESCE(""AllocationFee"", 0)) as ""Change"" ");
            sql.Append(@"FROM ""TransactionOps"" ");
            sql.Append(@"WHERE ""InitiatorId"" = @account ");
            sql.Append(@"AND ""Status"" = 1 ");

            if (from > 0)
                sql.Append($@"AND ""Level"" > {from} ");
            else if (to > 0)
                sql.Append($@"AND ""Level"" <= {to} ");

            sql.AppendLine();
            #endregion

            sql.Append("UNION ALL SELECT ");

            #region failed
            sql.Append(@"SUM(-""BakerFee"") as ""Change"" ");
            sql.Append(@"FROM ""TransactionOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");
            sql.Append(@"AND ""Status"" != 1 ");

            if (from > 0)
                sql.Append($@"AND ""Level"" > {from} ");
            else if (to > 0)
                sql.Append($@"AND ""Level"" <= {to} ");

            sql.AppendLine();
            #endregion
        }

        void SumReveals(StringBuilder sql, int from, int to)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"SUM(-""BakerFee"") as ""Change"" ");
            sql.Append(@"FROM ""RevealOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");

            if (from > 0)
                sql.Append($@"AND ""Level"" > {from} ");
            else if (to > 0)
                sql.Append($@"AND ""Level"" <= {to} ");

            sql.AppendLine();
        }

        void SumRegisterConstants(StringBuilder sql, int from, int to)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"SUM(-""BakerFee"" - COALESCE(""StorageFee"", 0)) as ""Change"" ");
            sql.Append(@"FROM ""RegisterConstantOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");

            if (from > 0)
                sql.Append($@"AND ""Level"" > {from} ");
            else if (to > 0)
                sql.Append($@"AND ""Level"" <= {to} ");

            sql.AppendLine();
        }

        void SumSetDepositsLimits(StringBuilder sql, int from, int to)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"SUM(-""BakerFee"") as ""Change"" ");
            sql.Append(@"FROM ""SetDepositsLimitOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");

            if (from > 0)
                sql.Append($@"AND ""Level"" > {from} ");
            else if (to > 0)
                sql.Append($@"AND ""Level"" <= {to} ");

            sql.AppendLine();
        }

        void SumTxRollupOriginationOps(StringBuilder sql, int from, int to)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"SUM(-""BakerFee"" - COALESCE(""AllocationFee"", 0)) as ""Change"" ");
            sql.Append(@"FROM ""TxRollupOriginationOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");

            if (from > 0)
                sql.Append($@"AND ""Level"" > {from} ");
            else if (to > 0)
                sql.Append($@"AND ""Level"" <= {to} ");

            sql.AppendLine();
        }

        void SumTxRollupSubmitBatchOps(StringBuilder sql, int from, int to)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"SUM(-""BakerFee"" - COALESCE(""StorageFee"", 0)) as ""Change"" ");
            sql.Append(@"FROM ""TxRollupSubmitBatchOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");

            if (from > 0)
                sql.Append($@"AND ""Level"" > {from} ");
            else if (to > 0)
                sql.Append($@"AND ""Level"" <= {to} ");

            sql.AppendLine();
        }

        void SumTxRollupCommitOps(StringBuilder sql, int from, int to)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"SUM(-""BakerFee"" - COALESCE(""StorageFee"", 0)) as ""Change"" ");
            sql.Append(@"FROM ""TxRollupCommitOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");

            if (from > 0)
                sql.Append($@"AND ""Level"" > {from} ");
            else if (to > 0)
                sql.Append($@"AND ""Level"" <= {to} ");

            sql.AppendLine();
        }

        void SumTxRollupFinalizeCommitmentOps(StringBuilder sql, int from, int to)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"SUM(-""BakerFee"" - COALESCE(""StorageFee"", 0)) as ""Change"" ");
            sql.Append(@"FROM ""TxRollupFinalizeCommitmentOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");

            if (from > 0)
                sql.Append($@"AND ""Level"" > {from} ");
            else if (to > 0)
                sql.Append($@"AND ""Level"" <= {to} ");

            sql.AppendLine();
        }

        void SumTxRollupRemoveCommitmentOps(StringBuilder sql, int from, int to)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"SUM(-""BakerFee"" - COALESCE(""StorageFee"", 0)) as ""Change"" ");
            sql.Append(@"FROM ""TxRollupRemoveCommitmentOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");

            if (from > 0)
                sql.Append($@"AND ""Level"" > {from} ");
            else if (to > 0)
                sql.Append($@"AND ""Level"" <= {to} ");

            sql.AppendLine();
        }

        void SumTxRollupReturnBondOps(StringBuilder sql, int from, int to)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"SUM(-""BakerFee"" - COALESCE(""StorageFee"", 0)) as ""Change"" ");
            sql.Append(@"FROM ""TxRollupReturnBondOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");

            if (from > 0)
                sql.Append($@"AND ""Level"" > {from} ");
            else if (to > 0)
                sql.Append($@"AND ""Level"" <= {to} ");

            sql.AppendLine();
        }

        void SumTxRollupRejectionOps(StringBuilder sql, int from, int to)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            #region sender
            sql.Append(@"SUM(-""BakerFee"" - COALESCE(""StorageFee"", 0) + ""Reward"") as ""Change"" ");
            sql.Append(@"FROM ""TxRollupRejectionOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");

            if (from > 0)
                sql.Append($@"AND ""Level"" > {from} ");
            else if (to > 0)
                sql.Append($@"AND ""Level"" <= {to} ");

            sql.AppendLine();
            #endregion

            sql.Append("UNION ALL SELECT ");

            #region committer
            sql.Append(@"SUM(-""Loss"") as ""Change"" ");
            sql.Append(@"FROM ""TxRollupRejectionOps"" ");
            sql.Append(@"WHERE ""CommitterId"" = @account ");

            if (from > 0)
                sql.Append($@"AND ""Level"" > {from} ");
            else if (to > 0)
                sql.Append($@"AND ""Level"" <= {to} ");

            sql.AppendLine();
            #endregion
        }

        void SumTxRollupDispatchTicketsOps(StringBuilder sql, int from, int to)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"SUM(-""BakerFee"" - COALESCE(""StorageFee"", 0)) as ""Change"" ");
            sql.Append(@"FROM ""TxRollupDispatchTicketsOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");

            if (from > 0)
                sql.Append($@"AND ""Level"" > {from} ");
            else if (to > 0)
                sql.Append($@"AND ""Level"" <= {to} ");

            sql.AppendLine();
        }

        void SumTransferTicketOps(StringBuilder sql, int from, int to)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"SUM(-""BakerFee"" - COALESCE(""StorageFee"", 0)) as ""Change"" ");
            sql.Append(@"FROM ""TransferTicketOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");

            if (from > 0)
                sql.Append($@"AND ""Level"" > {from} ");
            else if (to > 0)
                sql.Append($@"AND ""Level"" <= {to} ");

            sql.AppendLine();
        }

        void SumIncreasePaidStorageOps(StringBuilder sql, int from, int to)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"SUM(-""BakerFee"" - COALESCE(""StorageFee"", 0)) as ""Change"" ");
            sql.Append(@"FROM ""IncreasePaidStorageOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");

            if (from > 0)
                sql.Append($@"AND ""Level"" > {from} ");
            else if (to > 0)
                sql.Append($@"AND ""Level"" <= {to} ");

            sql.AppendLine();
        }

        void SumUpdateConsensusKeyOps(StringBuilder sql, int from, int to)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"SUM(-""BakerFee"") as ""Change"" ");
            sql.Append(@"FROM ""UpdateConsensusKeyOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");

            if (from > 0)
                sql.Append($@"AND ""Level"" > {from} ");
            else if (to > 0)
                sql.Append($@"AND ""Level"" <= {to} ");

            sql.AppendLine();
        }

        void SumDrainDelegateOps(StringBuilder sql, int from, int to)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            #region delegate
            sql.Append(@"SUM(-""Amount"" - ""Fee"") as ""Change"" ");
            sql.Append(@"FROM ""DrainDelegateOps"" ");
            sql.Append(@"WHERE ""DelegateId"" = @account ");

            if (from > 0)
                sql.Append($@"AND ""Level"" > {from} ");
            else if (to > 0)
                sql.Append($@"AND ""Level"" <= {to} ");

            sql.AppendLine();
            #endregion

            sql.Append("UNION ALL SELECT ");

            #region target
            sql.Append(@"SUM(""Amount"") as ""Change"" ");
            sql.Append(@"FROM ""DrainDelegateOps"" ");
            sql.Append(@"WHERE ""TargetId"" = @account ");

            if (from > 0)
                sql.Append($@"AND ""Level"" > {from} ");
            else if (to > 0)
                sql.Append($@"AND ""Level"" <= {to} ");

            sql.AppendLine();
            #endregion
        }

        void SumStakingOps(StringBuilder sql, int from, int to)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"SUM(-""BakerFee"") as ""Change"" ");
            sql.Append(@"FROM ""StakingOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");

            if (from > 0)
                sql.Append($@"AND ""Level"" > {from} ");
            else if (to > 0)
                sql.Append($@"AND ""Level"" <= {to} ");

            sql.AppendLine();
        }

        void SumSrAddMessagesOps(StringBuilder sql, int from, int to)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"SUM(-""BakerFee"") as ""Change"" ");
            sql.Append(@"FROM ""SmartRollupAddMessagesOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");

            if (from > 0)
                sql.Append($@"AND ""Level"" > {from} ");
            else if (to > 0)
                sql.Append($@"AND ""Level"" <= {to} ");

            sql.AppendLine();
        }

        void SumSrCementOps(StringBuilder sql, int from, int to)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"SUM(-""BakerFee"") as ""Change"" ");
            sql.Append(@"FROM ""SmartRollupCementOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");

            if (from > 0)
                sql.Append($@"AND ""Level"" > {from} ");
            else if (to > 0)
                sql.Append($@"AND ""Level"" <= {to} ");

            sql.AppendLine();
        }

        void SumSrExecuteOps(StringBuilder sql, int from, int to)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"SUM(-""BakerFee"" - COALESCE(""StorageFee"", 0)) as ""Change"" ");
            sql.Append(@"FROM ""SmartRollupExecuteOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");

            if (from > 0)
                sql.Append($@"AND ""Level"" > {from} ");
            else if (to > 0)
                sql.Append($@"AND ""Level"" <= {to} ");

            sql.AppendLine();
        }

        void SumSrOriginateOps(StringBuilder sql, int from, int to)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"SUM(-""BakerFee"" - COALESCE(""StorageFee"", 0)) as ""Change"" ");
            sql.Append(@"FROM ""SmartRollupOriginateOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");

            if (from > 0)
                sql.Append($@"AND ""Level"" > {from} ");
            else if (to > 0)
                sql.Append($@"AND ""Level"" <= {to} ");

            sql.AppendLine();
        }

        void SumSrPublishOps(StringBuilder sql, int from, int to)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"SUM(-""BakerFee"") as ""Change"" ");
            sql.Append(@"FROM ""SmartRollupPublishOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");

            if (from > 0)
                sql.Append($@"AND ""Level"" > {from} ");
            else if (to > 0)
                sql.Append($@"AND ""Level"" <= {to} ");

            sql.AppendLine();
        }

        void SumSrRecoverBondOps(StringBuilder sql, int from, int to)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"SUM(-""BakerFee"") as ""Change"" ");
            sql.Append(@"FROM ""SmartRollupRecoverBondOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");

            if (from > 0)
                sql.Append($@"AND ""Level"" > {from} ");
            else if (to > 0)
                sql.Append($@"AND ""Level"" <= {to} ");

            sql.AppendLine();
        }

        void SumSrRefuteOps(StringBuilder sql, int from, int to)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"SUM(-""BakerFee"") as ""Change"" ");
            sql.Append(@"FROM ""SmartRollupRefuteOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");

            if (from > 0)
                sql.Append($@"AND ""Level"" > {from} ");
            else if (to > 0)
                sql.Append($@"AND ""Level"" <= {to} ");

            sql.AppendLine();
        }

        void SumSrGames(StringBuilder sql, int from, int to)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            #region initiator
            sql.Append(@"SUM(COALESCE(g.""InitiatorReward"", 0) - COALESCE(g.""InitiatorLoss"", 0)) as ""Change"" ");
            sql.Append(@"FROM ""RefutationGames"" AS g ");
            sql.Append(@"INNER JOIN ""SmartRollupRefuteOps"" AS o ON o.""Id"" = g.""LastMoveId"" ");
            sql.Append(@"WHERE g.""InitiatorId"" = @account ");

            if (from > 0)
                sql.Append($@"AND o.""Level"" > {from} ");
            else if (to > 0)
                sql.Append($@"AND o.""Level"" <= {to} ");

            sql.AppendLine();
            #endregion

            sql.Append("UNION ALL SELECT ");

            #region opponent
            sql.Append(@"SUM(COALESCE(g.""OpponentReward"", 0) - COALESCE(g.""OpponentLoss"", 0)) as ""Change"" ");
            sql.Append(@"FROM ""RefutationGames"" AS g ");
            sql.Append(@"INNER JOIN ""SmartRollupRefuteOps"" AS o ON o.""Id"" = g.""LastMoveId"" ");
            sql.Append(@"WHERE g.""OpponentId"" = @account ");

            if (from > 0)
                sql.Append($@"AND o.""Level"" > {from} ");
            else if (to > 0)
                sql.Append($@"AND o.""Level"" <= {to} ");

            sql.AppendLine();
            #endregion
        }

        void SumRevelationPenalties(StringBuilder sql, int from, int to)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"SUM(-""Loss"") as ""Change"" ");
            sql.Append(@"FROM ""RevelationPenaltyOps"" ");
            sql.Append(@"WHERE ""BakerId"" = @account ");

            if (from > 0)
                sql.Append($@"AND ""Level"" > {from} ");
            else if (to > 0)
                sql.Append($@"AND ""Level"" <= {to} ");

            sql.AppendLine();
        }

        void SumMigrations(StringBuilder sql, int from, int to)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"SUM(""BalanceChange"") as ""Change"" ");
            sql.Append(@"FROM ""MigrationOps"" ");
            sql.Append(@"WHERE ""AccountId"" = @account ");

            if (from > 0)
                sql.Append($@"AND ""Level"" > {from} ");
            else if (to > 0)
                sql.Append($@"AND ""Level"" <= {to} ");

            sql.AppendLine();
        }
        #endregion

        #region select union
        string SelectUnion(RawAccount account)
        {
            var union = new StringBuilder();

            if (account.DelegationsCount > 0) UnionDelegations(union);
            if (account.OriginationsCount > 0) UnionOriginations(union);
            if (account.TransactionsCount > 0) UnionTransactions(union);
            if (account.RevealsCount > 0) UnionReveals(union);
            if (account.MigrationsCount > 0) UnionMigrations(union);
            if (account.TxRollupCommitCount > 0) UnionTxRollupCommitOps(union);
            if (account.TxRollupDispatchTicketsCount > 0) UnionTxRollupDispatchTicketsOps(union);
            if (account.TxRollupFinalizeCommitmentCount > 0) UnionTxRollupFinalizeCommitmentOps(union);
            if (account.TxRollupOriginationCount > 0) UnionTxRollupOriginationOps(union);
            if (account.TxRollupRejectionCount > 0) UnionTxRollupRejectionOps(union);
            if (account.TxRollupRemoveCommitmentCount > 0) UnionTxRollupRemoveCommitmentOps(union);
            if (account.TxRollupReturnBondCount > 0) UnionTxRollupReturnBondOps(union);
            if (account.TxRollupSubmitBatchCount > 0) UnionTxRollupSubmitBatchOps(union);
            if (account.TransferTicketCount > 0) UnionTransferTicketOps(union);
            if (account.IncreasePaidStorageCount > 0) UnionIncreasePaidStorageOps(union);
            if (account.SmartRollupAddMessagesCount > 0) UnionSrAddMessagesOps(union);
            if (account.SmartRollupCementCount > 0) UnionSrCementOps(union);
            if (account.SmartRollupExecuteCount > 0) UnionSrExecuteOps(union);
            if (account.SmartRollupOriginateCount > 0) UnionSrOriginateOps(union);
            if (account.SmartRollupPublishCount > 0) UnionSrPublishOps(union);
            if (account.SmartRollupRecoverBondCount > 0) UnionSrRecoverBondOps(union);
            if (account.SmartRollupRefuteCount > 0) UnionSrRefuteOps(union);
            if (account.RefutationGamesCount > 0) UnionSrGames(union);

            if (account is RawUser user)
            {
                if (user.Activated == true) UnionActivations(union);
                if (user.RegisterConstantsCount > 0) UnionRegisterConstants(union);
                if (user.SetDepositsLimitsCount > 0) UnionSetDepositsLimits(union);
                if (user.DrainDelegateCount > 0) UnionDrainDelegateOps(union);
                if (user.StakingOpsCount > 0) UnionStakingOps(union);
            }

            if (account is RawDelegate delegat)
            {
                if (delegat.EndorsingRewardsCount > 0) UnionEndorsingRewards(union);
                if (delegat.BlocksCount > 0) UnionBaking(union);
                if (delegat.EndorsementsCount > 0) UnionEndorsements(union);
                if (delegat.DoubleBakingCount > 0) UnionDoubleBaking(union);
                if (delegat.DoubleEndorsingCount > 0) UnionDoubleEndorsing(union);
                if (delegat.DoublePreendorsingCount > 0) UnionDoublePreendorsing(union);
                if (delegat.NonceRevelationsCount > 0) UnionNonceRevelations(union);
                if (delegat.VdfRevelationsCount > 0) UnionVdfRevelations(union);
                if (delegat.RevelationPenaltiesCount > 0) UnionRevelationPenalties(union);
                if (delegat.UpdateConsensusKeyCount > 0) UnionUpdateConsensusKeyOps(union);
            }

            return union.ToString();
        }

        void UnionEndorsingRewards(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"(""RewardLiquid"" + ""RewardStakedOwn"") as ""Change"" ");

            sql.Append(@"FROM ""EndorsingRewardOps"" ");
            sql.Append(@"WHERE ""BakerId"" = @account ");
            sql.Append(@"AND (""RewardLiquid"" != 0 OR ""RewardStakedOwn"" != 0) ");

            sql.AppendLine();
        }

        void UnionBaking(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            #region proposer
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"(""RewardLiquid"" + ""RewardStakedOwn"" + ""Fees"") as ""Change"" ");

            sql.Append(@"FROM ""Blocks"" ");
            sql.Append(@"WHERE ""ProposerId"" = @account ");

            sql.AppendLine();
            #endregion

            sql.Append("UNION ALL SELECT ");

            #region producer
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"(""BonusLiquid"" + ""BonusStakedOwn"") as ""Change"" ");

            sql.Append(@"FROM ""Blocks"" ");
            sql.Append(@"WHERE ""ProducerId"" = @account ");

            sql.AppendLine();
            #endregion
        }

        void UnionEndorsements(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"""Reward"" as ""Change"" ");

            sql.Append(@"FROM ""EndorsementOps"" ");
            sql.Append(@"WHERE ""DelegateId"" = @account ");
            sql.Append(@"AND ""Reward"" != 0 ");

            sql.AppendLine();
        }

        void UnionActivations(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"""Balance"" as ""Change"" ");

            sql.Append(@"FROM ""ActivationOps"" ");
            sql.Append(@"WHERE ""AccountId"" = @account ");

            sql.AppendLine();
        }

        void UnionDoubleBaking(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            #region accuser
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"""Reward"" as ""Change"" ");

            sql.Append(@"FROM ""DoubleBakingOps"" ");
            sql.Append(@"WHERE ""AccuserId"" = @account ");

            sql.AppendLine();
            #endregion

            sql.Append("UNION ALL SELECT ");

            #region offender
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"(-""LostStaked"" - ""LostUnstaked"") as ""Change"" ");

            sql.Append(@"FROM ""DoubleBakingOps"" ");
            sql.Append(@"WHERE ""OffenderId"" = @account ");

            sql.AppendLine();
            #endregion
        }

        void UnionDoubleEndorsing(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            #region accuser
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"""Reward"" as ""Change"" ");

            sql.Append(@"FROM ""DoubleEndorsingOps"" ");
            sql.Append(@"WHERE ""AccuserId"" = @account ");

            sql.AppendLine();
            #endregion

            sql.Append("UNION ALL SELECT ");

            #region offender
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"(-""LostStaked"" - ""LostUnstaked"") as ""Change"" ");

            sql.Append(@"FROM ""DoubleEndorsingOps"" ");
            sql.Append(@"WHERE ""OffenderId"" = @account ");

            sql.AppendLine();
            #endregion
        }

        void UnionDoublePreendorsing(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            #region accuser
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"""Reward"" as ""Change"" ");

            sql.Append(@"FROM ""DoublePreendorsingOps"" ");
            sql.Append(@"WHERE ""AccuserId"" = @account ");

            sql.AppendLine();
            #endregion

            sql.Append("UNION ALL SELECT ");

            #region offender
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"(-""LostStaked"" - ""LostUnstaked"") as ""Change"" ");

            sql.Append(@"FROM ""DoublePreendorsingOps"" ");
            sql.Append(@"WHERE ""OffenderId"" = @account ");

            sql.AppendLine();
            #endregion
        }

        void UnionNonceRevelations(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"(""RewardLiquid"" + ""RewardStakedOwn"") as ""Change"" ");

            sql.Append(@"FROM ""NonceRevelationOps"" ");
            sql.Append(@"WHERE ""BakerId"" = @account ");

            sql.AppendLine();
        }
        
        void UnionVdfRevelations(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"(""RewardLiquid"" + ""RewardStakedOwn"") as ""Change"" ");

            sql.Append(@"FROM ""VdfRevelationOps"" ");
            sql.Append(@"WHERE ""BakerId"" = @account ");

            sql.AppendLine();
        }

        void UnionDelegations(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"(-""BakerFee"") as ""Change"" ");

            sql.Append(@"FROM ""DelegationOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");

            sql.AppendLine();
        }

        void UnionOriginations(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            #region originated
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"""Balance"" as ""Change"" ");

            sql.Append(@"FROM ""OriginationOps"" ");
            sql.Append(@"WHERE ""ContractId"" = @account ");

            sql.AppendLine();
            #endregion

            sql.Append("UNION ALL SELECT ");

            #region sent
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"(-""Balance"" -(CASE WHEN ""Nonce"" is NULL THEN (""BakerFee"" + COALESCE(""StorageFee"", 0) + COALESCE(""AllocationFee"", 0)) ELSE 0 END)) as ""Change"" ");

            sql.Append(@"FROM ""OriginationOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");
            sql.Append(@"AND ""Status"" = 1 ");

            sql.AppendLine();
            #endregion

            sql.Append("UNION ALL SELECT ");

            #region internal
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"(-COALESCE(""StorageFee"", 0) - COALESCE(""AllocationFee"", 0)) as ""Change"" ");

            sql.Append(@"FROM ""OriginationOps"" ");
            sql.Append(@"WHERE ""InitiatorId"" = @account ");
            sql.Append(@"AND ""Status"" = 1 ");

            sql.AppendLine();
            #endregion

            sql.Append("UNION ALL SELECT ");

            #region failed
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"(-""BakerFee"") as ""Change"" ");

            sql.Append(@"FROM ""OriginationOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");
            sql.Append(@"AND ""Status"" != 1 ");

            sql.AppendLine();
            #endregion
        }

        void UnionTransactions(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            #region received
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"""Amount"" as ""Change"" ");

            sql.Append(@"FROM ""TransactionOps"" ");
            sql.Append(@"WHERE ""TargetId"" = @account ");
            sql.Append(@"AND ""Status"" = 1 ");

            sql.AppendLine();
            #endregion

            sql.Append("UNION ALL SELECT ");

            #region sent
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"(-""Amount"" - (CASE WHEN ""Nonce"" is NULL THEN (""BakerFee"" + COALESCE(""StorageFee"", 0) + COALESCE(""AllocationFee"", 0)) ELSE 0 END)) as ""Change"" ");

            sql.Append(@"FROM ""TransactionOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");
            sql.Append(@"AND ""Status"" = 1 ");

            sql.AppendLine();
            #endregion

            sql.Append("UNION ALL SELECT ");

            #region internal
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"(-COALESCE(""StorageFee"", 0) - COALESCE(""AllocationFee"", 0)) as ""Change"" ");

            sql.Append(@"FROM ""TransactionOps"" ");
            sql.Append(@"WHERE ""InitiatorId"" = @account ");
            sql.Append(@"AND ""Status"" = 1 ");

            sql.AppendLine();
            #endregion

            sql.Append("UNION ALL SELECT ");

            #region failed
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"(-""BakerFee"") as ""Change"" ");

            sql.Append(@"FROM ""TransactionOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");
            sql.Append(@"AND ""Status"" != 1 ");

            sql.AppendLine();
            #endregion
        }

        void UnionReveals(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"(-""BakerFee"") as ""Change"" ");

            sql.Append(@"FROM ""RevealOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");

            sql.AppendLine();
        }

        void UnionRegisterConstants(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"(-""BakerFee"" - COALESCE(""StorageFee"", 0)) as ""Change"" ");

            sql.Append(@"FROM ""RegisterConstantOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");

            sql.AppendLine();
        }

        void UnionSetDepositsLimits(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"(-""BakerFee"") as ""Change"" ");

            sql.Append(@"FROM ""SetDepositsLimitOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");

            sql.AppendLine();
        }

        void UnionTxRollupOriginationOps(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"(-""BakerFee"" - COALESCE(""AllocationFee"", 0)) as ""Change"" ");

            sql.Append(@"FROM ""TxRollupOriginationOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");

            sql.AppendLine();
        }

        void UnionTxRollupSubmitBatchOps(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"(-""BakerFee"" - COALESCE(""StorageFee"", 0)) as ""Change"" ");

            sql.Append(@"FROM ""TxRollupSubmitBatchOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");

            sql.AppendLine();
        }

        void UnionTxRollupCommitOps(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"(-""BakerFee"" - COALESCE(""StorageFee"", 0)) as ""Change"" ");

            sql.Append(@"FROM ""TxRollupCommitOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");

            sql.AppendLine();
        }

        void UnionTxRollupFinalizeCommitmentOps(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"(-""BakerFee"" - COALESCE(""StorageFee"", 0)) as ""Change"" ");

            sql.Append(@"FROM ""TxRollupFinalizeCommitmentOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");

            sql.AppendLine();
        }

        void UnionTxRollupRemoveCommitmentOps(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"(-""BakerFee"" - COALESCE(""StorageFee"", 0)) as ""Change"" ");

            sql.Append(@"FROM ""TxRollupRemoveCommitmentOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");

            sql.AppendLine();
        }

        void UnionTxRollupReturnBondOps(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"(-""BakerFee"" - COALESCE(""StorageFee"", 0)) as ""Change"" ");

            sql.Append(@"FROM ""TxRollupReturnBondOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");

            sql.AppendLine();
        }

        void UnionTxRollupRejectionOps(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            #region sender
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"(""Reward"" - ""BakerFee"" - COALESCE(""StorageFee"", 0)) as ""Change"" ");

            sql.Append(@"FROM ""TxRollupRejectionOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");

            sql.AppendLine();
            #endregion

            sql.Append("UNION ALL SELECT ");

            #region committer
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"(-""Loss"") as ""Change"" ");

            sql.Append(@"FROM ""TxRollupRejectionOps"" ");
            sql.Append(@"WHERE ""CommitterId"" = @account ");

            sql.AppendLine();
            #endregion
        }

        void UnionTxRollupDispatchTicketsOps(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"(-""BakerFee"" - COALESCE(""StorageFee"", 0)) as ""Change"" ");

            sql.Append(@"FROM ""TxRollupDispatchTicketsOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");

            sql.AppendLine();
        }

        void UnionTransferTicketOps(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"(-""BakerFee"" - COALESCE(""StorageFee"", 0)) as ""Change"" ");

            sql.Append(@"FROM ""TransferTicketOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");

            sql.AppendLine();
        }

        void UnionIncreasePaidStorageOps(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"(-""BakerFee"" - COALESCE(""StorageFee"", 0)) as ""Change"" ");

            sql.Append(@"FROM ""IncreasePaidStorageOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");

            sql.AppendLine();
        }

        void UnionUpdateConsensusKeyOps(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"(-""BakerFee"") as ""Change"" ");

            sql.Append(@"FROM ""UpdateConsensusKeyOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");

            sql.AppendLine();
        }

        void UnionDrainDelegateOps(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            #region delegate
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"(-""Amount"" - ""Fee"") as ""Change"" ");

            sql.Append(@"FROM ""DrainDelegateOps"" ");
            sql.Append(@"WHERE ""DelegateId"" = @account ");

            sql.AppendLine();
            #endregion

            sql.Append("UNION ALL SELECT ");

            #region target
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"(""Amount"") as ""Change"" ");

            sql.Append(@"FROM ""DrainDelegateOps"" ");
            sql.Append(@"WHERE ""TargetId"" = @account ");

            sql.AppendLine();
            #endregion
        }

        void UnionStakingOps(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"(-""BakerFee"") as ""Change"" ");

            sql.Append(@"FROM ""StakingOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");

            sql.AppendLine();
        }

        void UnionSrAddMessagesOps(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"(-""BakerFee"") as ""Change"" ");

            sql.Append(@"FROM ""SmartRollupAddMessagesOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");

            sql.AppendLine();
        }

        void UnionSrCementOps(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"(-""BakerFee"") as ""Change"" ");

            sql.Append(@"FROM ""SmartRollupCementOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");

            sql.AppendLine();
        }

        void UnionSrExecuteOps(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"(-""BakerFee"" - COALESCE(""StorageFee"", 0)) as ""Change"" ");

            sql.Append(@"FROM ""SmartRollupExecuteOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");

            sql.AppendLine();
        }

        void UnionSrOriginateOps(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"(-""BakerFee"" - COALESCE(""StorageFee"", 0)) as ""Change"" ");

            sql.Append(@"FROM ""SmartRollupOriginateOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");

            sql.AppendLine();
        }

        void UnionSrPublishOps(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"(-""BakerFee"") as ""Change"" ");

            sql.Append(@"FROM ""SmartRollupPublishOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");

            sql.AppendLine();
        }

        void UnionSrRecoverBondOps(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"(-""BakerFee"") as ""Change"" ");

            sql.Append(@"FROM ""SmartRollupRecoverBondOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");

            sql.AppendLine();
        }

        void UnionSrRefuteOps(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"(-""BakerFee"") as ""Change"" ");

            sql.Append(@"FROM ""SmartRollupRefuteOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");

            sql.AppendLine();
        }

        void UnionSrGames(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            #region initiator
            sql.Append(@"o.""Level"" as ""Level"", ");
            sql.Append(@"(COALESCE(g.""InitiatorReward"", 0) - COALESCE(g.""InitiatorLoss"", 0)) as ""Change"" ");

            sql.Append(@"FROM ""RefutationGames"" AS g ");
            sql.Append(@"INNER JOIN ""SmartRollupRefuteOps"" AS o ON o.""Id"" = g.""LastMoveId"" ");
            sql.Append(@"WHERE g.""InitiatorId"" = @account ");
            sql.Append(@"AND (g.""InitiatorReward"" IS NOT NULL OR g.""InitiatorLoss"" IS NOT NULL) ");

            sql.AppendLine();
            #endregion

            sql.Append("UNION ALL SELECT ");

            #region opponent
            sql.Append(@"o.""Level"" as ""Level"", ");
            sql.Append(@"(COALESCE(g.""OpponentReward"", 0) - COALESCE(g.""OpponentLoss"", 0)) as ""Change"" ");

            sql.Append(@"FROM ""RefutationGames"" AS g ");
            sql.Append(@"INNER JOIN ""SmartRollupRefuteOps"" AS o ON o.""Id"" = g.""LastMoveId"" ");
            sql.Append(@"WHERE g.""OpponentId"" = @account ");
            sql.Append(@"AND (g.""OpponentReward"" IS NOT NULL OR g.""OpponentLoss"" IS NOT NULL) ");

            sql.AppendLine();
            #endregion
        }

        void UnionRevelationPenalties(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"(-""Loss"") as ""Change"" ");

            sql.Append(@"FROM ""RevelationPenaltyOps"" ");
            sql.Append(@"WHERE ""BakerId"" = @account ");

            sql.AppendLine();
        }

        void UnionMigrations(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"""BalanceChange"" as ""Change"" ");

            sql.Append(@"FROM ""MigrationOps"" ");
            sql.Append(@"WHERE ""AccountId"" = @account ");

            sql.AppendLine();
        }
        #endregion
    }
}
