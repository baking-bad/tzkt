using Dapper;
using Npgsql;
using Tzkt.Api.Models;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Repositories
{
    public class DalRepository
    {
        readonly NpgsqlDataSource DataSource;
        readonly AccountsCache Accounts;
        readonly ProtocolsCache Protocols;
        readonly QuotesCache Quotes;

        public DalRepository(NpgsqlDataSource dataSource, AccountsCache accounts, ProtocolsCache protocols, QuotesCache quotes)
        {
            DataSource = dataSource;
            Accounts = accounts;
            Protocols = protocols;
            Quotes = quotes;
        }

        #region commiments
        public async Task<int> GetCommitmentsCount(
            DalCommitmentHashParameter hash,
            Int32Parameter level,
            Int32Parameter slotIndex,
            AccountParameter publisher)
        {
            var sql = new SqlBuilder($"""
                SELECT COUNT(*) FROM "DalCommitmentStatus" AS dc
                INNER JOIN "DalPublishCommitmentOps" AS op ON dc."PublishmentId" = op."Id"
                """)
                .FilterA(@"op.""Commitment""", hash)
                .FilterA(@"op.""Level""", level)
                .FilterA(@"op.""Slot""", slotIndex)
                .FilterA(@"op.""SenderId""", publisher);

            await using var db = await DataSource.OpenConnectionAsync();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<DalCommitment>> GetCommitments(
            DalCommitmentHashParameter hash,
            Int32Parameter level,
            Int32Parameter slotIndex,
            AccountParameter publisher,
            SortParameter sort,
            OffsetParameter offset,
            int limit)
        {
            var sql = new SqlBuilder($"""
                SELECT     op.*
                FROM       "DalCommitmentStatus" AS dc
                INNER JOIN "DalPublishCommitmentOps" AS op ON dc."PublishmentId" = op."Id"
                """)
                .FilterA(@"op.""Commitment""", hash)
                .FilterA(@"op.""Level""", level)
                .FilterA(@"op.""Slot""", slotIndex)
                .FilterA(@"op.""SenderId""", publisher)
                .Take(new Pagination { sort = sort, offset = offset, limit = limit }, x => x switch
                {
                    "slotIndex" => (@"op.""Slot""", @"op.""Slot"""),
                    "level" or _  => (@"op.""Level""", @"op.""Level""")
                }, @"op.""Level""");

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row => new DalCommitment
            {
                Level = row.Level,
                SlotIndex = row.Slot,
                Hash = row.Commitment,
                Publisher = Accounts.GetAlias(row.SenderId)
            });
        }

        public async Task<object[][]> GetCommitments(
            DalCommitmentHashParameter hash,
            Int32Parameter level,
            Int32Parameter slotIndex,
            AccountParameter publisher,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string[] fields)
        {
            var columns = new HashSet<string>(fields.Length);

            foreach (var field in fields)
            {
                switch (field)
                {
                    case "level": columns.Add(@"op.""Level"""); break;
                    case "slotIndex": columns.Add(@"op.""Slot"""); break;
                    case "hash": columns.Add(@"op.""Commitment"""); break;
                    case "publisher": columns.Add(@"op.""SenderId"""); break;
                }
            }

            if (columns.Count == 0)
                return Array.Empty<object[]>();

            var sql = new SqlBuilder($"""
                SELECT {string.Join(',', columns)}
                FROM       "DalCommitmentStatus" AS dc
                INNER JOIN "DalPublishCommitmentOps" AS op ON dc."PublishmentId" = op."Id"
                """)
                .FilterA(@"op.""Commitment""", hash)
                .FilterA(@"op.""Level""", level)
                .FilterA(@"op.""Slot""", slotIndex)
                .FilterA(@"op.""SenderId""", publisher)
                .Take(new Pagination { sort = sort, offset = offset, limit = limit }, x => x switch
                {
                    "slotIndex" => (@"op.""Slot""", @"op.""Slot"""),
                    "level" or _  => (@"op.""Level""", @"op.""Level""")
                }, @"op.""Level""");

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

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
                    case "slotIndex":
                        foreach (var row in rows)
                            result[j++][i] = row.Slot;
                        break;
                    case "hash":
                        foreach (var row in rows)
                            result[j++][i] = row.Commitment;
                        break;
                    case "publisher":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.SenderId);
                        break;
                }
            }

            return result;
        }

        public async Task<object[]> GetCommitments(
            DalCommitmentHashParameter hash,
            Int32Parameter level,
            Int32Parameter slotIndex,
            AccountParameter publisher,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string field)
        {
            var columns = new HashSet<string>(1);

            switch (field)
            {
                case "level": columns.Add(@"op.""Level"""); break;
                case "slotIndex": columns.Add(@"op.""Slot"""); break;
                case "hash": columns.Add(@"op.""Commitment"""); break;
                case "publisher": columns.Add(@"op.""SenderId"""); break;
            }

            if (columns.Count == 0)
                return Array.Empty<object>();

            var sql = new SqlBuilder($"""
                SELECT {string.Join(',', columns)}
                FROM       "DalCommitmentStatus" AS dc
                INNER JOIN "DalPublishCommitmentOps" AS op ON dc."PublishmentId" = op."Id"
                """)
                .FilterA(@"op.""Commitment""", hash)
                .FilterA(@"op.""Level""", level)
                .FilterA(@"op.""Slot""", slotIndex)
                .FilterA(@"op.""SenderId""", publisher)
                .Take(new Pagination { sort = sort, offset = offset, limit = limit }, x => x switch
                {
                    "slotIndex" => (@"op.""Slot""", @"op.""Slot"""),
                    "level" or _  => (@"op.""Level""", @"op.""Level""")
                }, @"op.""Level""");

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            var result = new object[rows.Count()];
            var j = 0;

            switch (field)
            {
                case "level":
                    foreach (var row in rows)
                        result[j++] = row.Level;
                    break;
                case "slotIndex":
                    foreach (var row in rows)
                        result[j++] = row.Slot;
                    break;
                case "hash":
                    foreach (var row in rows)
                        result[j++] = row.Commitment;
                    break;
                case "publisher":
                    foreach (var row in rows)
                        result[j++] = Accounts.GetAlias(row.SenderId);
                    break;
            }

            return result;
        }
        #endregion
    }
}
