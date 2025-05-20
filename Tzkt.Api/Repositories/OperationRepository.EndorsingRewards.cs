using Dapper;
using Tzkt.Api.Models;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Repositories
{
    public partial class OperationRepository
    {
        public async Task<int> GetEndorsingRewardsCount(
            Int32Parameter? level,
            TimestampParameter? timestamp)
        {
            var sql = new SqlBuilder(@"SELECT COUNT(*) FROM ""EndorsingRewardOps""")
                .Filter("Level", level)
                .Filter("Level", timestamp);

            await using var db = await DataSource.OpenConnectionAsync();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        public async Task<EndorsingRewardOperation?> GetEndorsingReward(long id, Symbols quote)
        {
            var sql = $@"
                SELECT      o.*, b.""Hash""
                FROM        ""EndorsingRewardOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                WHERE       o.""Id"" = @id
                LIMIT       1";

            await using var db = await DataSource.OpenConnectionAsync();
            var row = await db.QueryFirstOrDefaultAsync(sql, new { id });
            if (row == null) return null;

            return new EndorsingRewardOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Baker = Accounts.GetAlias(row.BakerId),
                Expected = row.Expected,
                RewardDelegated = row.RewardDelegated,
                RewardStakedOwn = row.RewardStakedOwn,
                RewardStakedEdge = row.RewardStakedEdge,
                RewardStakedShared = row.RewardStakedShared,
                Quote = Quotes.Get(quote, row.Level)
            };
        }

        public async Task<IEnumerable<Activity>> GetEndorsingRewardOpsActivity(
            List<RawAccount> accounts,
            ActivityRole roles,
            TimestampParameter? timestamp,
            Pagination pagination,
            Symbols quote)
        {
            List<int>? ids = null;

            foreach (var account in accounts)
            {
                if (account is not RawDelegate baker || baker.EndorsingRewardsCount == 0)
                    continue;

                if ((roles & ActivityRole.Target) != 0)
                {
                    ids ??= new(accounts.Count);
                    ids.Add(account.Id);
                }
            }

            if (ids == null)
                return [];

            var or = new OrParameter((@"o.""BakerId""", ids));

            return await GetEndorsingRewards(
                or,
                null, null, null,
                timestamp,
                pagination.sort,
                pagination.offset,
                pagination.limit,
                quote);
        }

        public async Task<IEnumerable<EndorsingRewardOperation>> GetEndorsingRewards(
            OrParameter? or,
            Int64Parameter? id,
            AccountParameter? baker,
            Int32Parameter? level,
            TimestampParameter? timestamp,
            SortParameter? sort,
            OffsetParameter? offset,
            int limit,
            Symbols quote)
        {
            var sql = new SqlBuilder(@"SELECT o.*, b.""Hash"" FROM ""EndorsingRewardOps"" AS o INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""")
                .FilterA(or)
                .FilterA(@"o.""Id""", id)
                .FilterA(@"o.""BakerId""", baker)
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Level""", timestamp)
                .Take(sort, offset, limit, x => x == "level" ? ("Id", "Level") : ("Id", "Id"), "o");

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row => new EndorsingRewardOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Baker = Accounts.GetAlias(row.BakerId),
                Expected = row.Expected,
                RewardDelegated = row.RewardDelegated,
                RewardStakedOwn = row.RewardStakedOwn,
                RewardStakedEdge = row.RewardStakedEdge,
                RewardStakedShared = row.RewardStakedShared,
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<object?[][]> GetEndorsingRewards(
            Int64Parameter? id,
            AccountParameter? baker,
            Int32Parameter? level,
            TimestampParameter? timestamp,
            SortParameter? sort,
            OffsetParameter? offset,
            int limit,
            string[] fields,
            Symbols quote)
        {
            var columns = new HashSet<string>(fields.Length);
            var joins = new HashSet<string>(1);

            foreach (var field in fields)
            {
                switch (field)
                {
                    case "id": columns.Add(@"o.""Id"""); break;
                    case "level": columns.Add(@"o.""Level"""); break;
                    case "timestamp": columns.Add(@"o.""Timestamp"""); break;
                    case "baker": columns.Add(@"o.""BakerId"""); break;
                    case "expected": columns.Add(@"o.""Expected"""); break;
                    case "rewardDelegated": columns.Add(@"o.""RewardDelegated"""); break;
                    case "rewardStakedOwn": columns.Add(@"o.""RewardStakedOwn"""); break;
                    case "rewardStakedEdge": columns.Add(@"o.""RewardStakedEdge"""); break;
                    case "rewardStakedShared": columns.Add(@"o.""RewardStakedShared"""); break;
                    case "block":
                        columns.Add(@"b.""Hash""");
                        joins.Add(@"INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""");
                        break;
                    case "quote": columns.Add(@"o.""Level"""); break;
                }
            }

            if (columns.Count == 0)
                return [];

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""EndorsingRewardOps"" as o {string.Join(' ', joins)}")
                .FilterA(@"o.""Id""", id)
                .FilterA(@"o.""BakerId""", baker)
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Level""", timestamp)
                .Take(sort, offset, limit, x => x == "level" ? ("Id", "Level") : ("Id", "Id"), "o");

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            var result = new object?[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object?[fields.Length];

            for (int i = 0, j = 0; i < fields.Length; j = 0, i++)
            {
                switch (fields[i])
                {
                    case "id":
                        foreach (var row in rows)
                            result[j++][i] = row.Id;
                        break;
                    case "level":
                        foreach (var row in rows)
                            result[j++][i] = row.Level;
                        break;
                    case "block":
                        foreach (var row in rows)
                            result[j++][i] = row.Hash;
                        break;
                    case "timestamp":
                        foreach (var row in rows)
                            result[j++][i] = row.Timestamp;
                        break;
                    case "baker":
                        foreach (var row in rows)
                            result[j++][i] = await Accounts.GetAliasAsync(row.BakerId);
                        break;
                    case "expected":
                        foreach (var row in rows)
                            result[j++][i] = row.Expected;
                        break;
                    case "rewardDelegated":
                        foreach (var row in rows)
                            result[j++][i] = row.RewardDelegated;
                        break;
                    case "rewardStakedOwn":
                        foreach (var row in rows)
                            result[j++][i] = row.RewardStakedOwn;
                        break;
                    case "rewardStakedEdge":
                        foreach (var row in rows)
                            result[j++][i] = row.RewardStakedEdge;
                        break;
                    case "rewardStakedShared":
                        foreach (var row in rows)
                            result[j++][i] = row.RewardStakedShared;
                        break;
                    case "quote":
                        foreach (var row in rows)
                            result[j++][i] = Quotes.Get(quote, row.Level);
                        break;
                }
            }

            return result;
        }

        public async Task<object?[]> GetEndorsingRewards(
            Int64Parameter? id,
            AccountParameter? baker,
            Int32Parameter? level,
            TimestampParameter? timestamp,
            SortParameter? sort,
            OffsetParameter? offset,
            int limit,
            string field,
            Symbols quote)
        {
            var columns = new HashSet<string>(1);
            var joins = new HashSet<string>(1);

            switch (field)
            {
                case "id": columns.Add(@"o.""Id"""); break;
                case "level": columns.Add(@"o.""Level"""); break;
                case "timestamp": columns.Add(@"o.""Timestamp"""); break;
                case "baker": columns.Add(@"o.""BakerId"""); break;
                case "expected": columns.Add(@"o.""Expected"""); break;
                case "rewardDelegated": columns.Add(@"o.""RewardDelegated"""); break;
                case "rewardStakedOwn": columns.Add(@"o.""RewardStakedOwn"""); break;
                case "rewardStakedEdge": columns.Add(@"o.""RewardStakedEdge"""); break;
                case "rewardStakedShared": columns.Add(@"o.""RewardStakedShared"""); break;
                case "block":
                    columns.Add(@"b.""Hash""");
                    joins.Add(@"INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""");
                    break;
                case "quote": columns.Add(@"o.""Level"""); break;
            }

            if (columns.Count == 0)
                return [];

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""EndorsingRewardOps"" as o {string.Join(' ', joins)}")
                .FilterA(@"o.""Id""", id)
                .FilterA(@"o.""BakerId""", baker)
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Level""", timestamp)
                .Take(sort, offset, limit, x => x == "level" ? ("Id", "Level") : ("Id", "Id"), "o");

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            //TODO: optimize memory allocation
            var result = new object?[rows.Count()];
            var j = 0;

            switch (field)
            {
                case "id":
                    foreach (var row in rows)
                        result[j++] = row.Id;
                    break;
                case "level":
                    foreach (var row in rows)
                        result[j++] = row.Level;
                    break;
                case "block":
                    foreach (var row in rows)
                        result[j++] = row.Hash;
                    break;
                case "timestamp":
                    foreach (var row in rows)
                        result[j++] = row.Timestamp;
                    break;
                case "baker":
                    foreach (var row in rows)
                        result[j++] = await Accounts.GetAliasAsync(row.BakerId);
                    break;
                case "expected":
                    foreach (var row in rows)
                        result[j++] = row.Expected;
                    break;
                case "rewardDelegated":
                    foreach (var row in rows)
                        result[j++] = row.RewardDelegated;
                    break;
                case "rewardStakedOwn":
                    foreach (var row in rows)
                        result[j++] = row.RewardStakedOwn;
                    break;
                case "rewardStakedEdge":
                    foreach (var row in rows)
                        result[j++] = row.RewardStakedEdge;
                    break;
                case "rewardStakedShared":
                    foreach (var row in rows)
                        result[j++] = row.RewardStakedShared;
                    break;
                case "quote":
                    foreach (var row in rows)
                        result[j++] = Quotes.Get(quote, row.Level);
                    break;
            }

            return result;
        }
    }
}
