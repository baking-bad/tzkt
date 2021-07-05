using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Dapper;

using Tzkt.Api.Models;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Repositories
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
            if (limit == 1 && offset == 0 && sort.Desc != null)
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

            if (account is RawUser user)
            {
                if (user.Activated == true) SumActivations(union, from, to);
            }

            if (account is RawDelegate delegat)
            {
                if (delegat.BlocksCount > 0) SumBaking(union, from, to);
                if (delegat.EndorsementsCount > 0) SumEndorsements(union, from, to);
                if (delegat.DoubleBakingCount > 0) SumDoubleBaking(union, from, to);
                if (delegat.DoubleEndorsingCount > 0) SumDoubleEndorsing(union, from, to);
                if (delegat.NonceRevelationsCount > 0) SumNonceRevelations(union, from, to);
                if (delegat.RevelationPenaltiesCount > 0) SumRevelationPenalties(union, from, to);
            }

            return union.ToString();
        }

        void SumBaking(StringBuilder sql, int from, int to)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"SUM(""Reward"" + ""Fees"") as ""Change"" ");
            sql.Append(@"FROM ""Blocks"" ");
            sql.Append(@"WHERE ""BakerId"" = @account ");

            if (from > 0)
                sql.Append($@"AND ""Level"" > {from} ");
            else if (to > 0)
                sql.Append($@"AND ""Level"" <= {to} ");

            sql.AppendLine();
        }

        void SumEndorsements(StringBuilder sql, int from, int to)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"SUM(""Reward"") as ""Change"" ");
            sql.Append(@"FROM ""EndorsementOps"" ");
            sql.Append(@"WHERE ""DelegateId"" = @account ");

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
            sql.Append(@"SUM(""AccuserReward"") as ""Change"" ");
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
            sql.Append(@"SUM(-""OffenderLostDeposit"" - ""OffenderLostReward"" - ""OffenderLostFee"") as ""Change"" ");
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
            sql.Append(@"SUM(""AccuserReward"") as ""Change"" ");
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
            sql.Append(@"SUM(-""OffenderLostDeposit"" - ""OffenderLostReward"" - ""OffenderLostFee"") as ""Change"" ");
            sql.Append(@"FROM ""DoubleEndorsingOps"" ");
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

            sql.Append(@"COUNT(*) * 125000 "); // TODO: get reward amount from protocol constants
            sql.Append(@"FROM ""NonceRevelationOps"" ");
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

        void SumRevelationPenalties(StringBuilder sql, int from, int to)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"SUM(-""LostReward"" - ""LostFees"") as ""Change"" ");
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

            if (account is RawUser user)
            {
                if (user.Activated == true) UnionActivations(union);
            }

            if (account is RawDelegate delegat)
            {
                if (delegat.BlocksCount > 0) UnionBaking(union);
                if (delegat.EndorsementsCount > 0) UnionEndorsements(union);
                if (delegat.DoubleBakingCount > 0) UnionDoubleBaking(union);
                if (delegat.DoubleEndorsingCount > 0) UnionDoubleEndorsing(union);
                if (delegat.NonceRevelationsCount > 0) UnionNonceRevelations(union);
                if (delegat.RevelationPenaltiesCount > 0) UnionRevelationPenalties(union);
            }

            return union.ToString();
        }

        void UnionBaking(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"(""Reward"" + ""Fees"") as ""Change"" ");

            sql.Append(@"FROM ""Blocks"" ");
            sql.Append(@"WHERE ""BakerId"" = @account ");

            sql.AppendLine();
        }

        void UnionEndorsements(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"""Reward"" as ""Change"" ");

            sql.Append(@"FROM ""EndorsementOps"" ");
            sql.Append(@"WHERE ""DelegateId"" = @account ");

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
            sql.Append(@"""AccuserReward"" as ""Change"" ");

            sql.Append(@"FROM ""DoubleBakingOps"" ");
            sql.Append(@"WHERE ""AccuserId"" = @account ");

            sql.AppendLine();
            #endregion

            sql.Append("UNION ALL SELECT ");

            #region offender
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"(-""OffenderLostDeposit"" - ""OffenderLostReward"" - ""OffenderLostFee"") as ""Change"" ");

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
            sql.Append(@"""AccuserReward"" as ""Change"" ");

            sql.Append(@"FROM ""DoubleEndorsingOps"" ");
            sql.Append(@"WHERE ""AccuserId"" = @account ");

            sql.AppendLine();
            #endregion

            sql.Append("UNION ALL SELECT ");

            #region offender
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"(-""OffenderLostDeposit"" - ""OffenderLostReward"" - ""OffenderLostFee"") as ""Change"" ");

            sql.Append(@"FROM ""DoubleEndorsingOps"" ");
            sql.Append(@"WHERE ""OffenderId"" = @account ");

            sql.AppendLine();
            #endregion
        }

        void UnionNonceRevelations(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"125000::bigint as ""Change"" "); // TODO: get reward amount from protocol constants

            sql.Append(@"FROM ""NonceRevelationOps"" ");
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

        void UnionRevelationPenalties(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"(-""LostReward"" - ""LostFees"") as ""Change"" ");

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
