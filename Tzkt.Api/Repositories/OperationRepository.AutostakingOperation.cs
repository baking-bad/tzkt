using Dapper;
using Tzkt.Api.Models;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Repositories
{
    public partial class OperationRepository
    {
        public async Task<IEnumerable<Activity>> GetAutostakingOpsActivity(
            List<RawAccount> accounts,
            ActivityRole roles,
            TimestampParameter? timestamp,
            Pagination pagination,
            Symbols quote)
        {
            List<int>? ids = null;

            foreach (var account in accounts)
            {
                if (account is not RawDelegate baker || baker.AutostakingOpsCount == 0)
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

            return await GetAutostakingOps(new() { or = or, timestamp = timestamp }, pagination, quote);
        }

        public async Task<int> GetAutostakingOpsCount(AutostakingOperationFilter filter)
        {
            var sql = new SqlBuilder("""
                SELECT COUNT(*)
                FROM "AutostakingOps" as o
                """)
                .FilterA(filter.or)
                .FilterA(@"o.""Id""", filter.id)
                .FilterA(@"o.""Level""", filter.level)
                .FilterA(@"o.""Level""", filter.timestamp)
                .FilterA(@"o.""BakerId""", filter.baker)
                .FilterA(@"o.""Action""", filter.action)
                .FilterA(@"o.""Amount""", filter.amount)
                .FilterA(@"o.""StakingUpdatesCount""", filter.stakingUpdatesCount);

            await using var db = await DataSource.OpenConnectionAsync();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        async Task<IEnumerable<dynamic>> QueryAutostakingOps(AutostakingOperationFilter filter, Pagination pagination, List<SelectionField>? fields = null)
        {
            var select = """
                o."Id",
                o."Level",
                o."BakerId",
                o."Action",
                o."Amount",
                o."StakingUpdatesCount"
            """;

            if (fields != null)
            {
                var columns = new HashSet<string>(fields.Count);
                foreach (var field in fields)
                {
                    switch (field.Field)
                    {
                        case "id": columns.Add(@"o.""Id"""); break;
                        case "level": columns.Add(@"o.""Level"""); break;
                        case "timestamp": columns.Add(@"o.""Level"""); break;
                        case "baker": columns.Add(@"o.""BakerId"""); break;
                        case "action": columns.Add(@"o.""Action"""); break;
                        case "amount": columns.Add(@"o.""Amount"""); break;
                        case "stakingUpdatesCount": columns.Add(@"o.""StakingUpdatesCount"""); break;
                        case "quote": columns.Add(@"o.""Level"""); break;
                    }
                }

                if (columns.Count == 0)
                    return [];

                select = string.Join(',', columns);
            }

            var sql = new SqlBuilder($"""
                SELECT {select}
                FROM "AutostakingOps" as o
                """)
                .FilterA(filter.or)
                .FilterA(@"o.""Id""", filter.id)
                .FilterA(@"o.""Level""", filter.level)
                .FilterA(@"o.""Level""", filter.timestamp)
                .FilterA(@"o.""BakerId""", filter.baker)
                .FilterA(@"o.""Action""", filter.action)
                .FilterA(@"o.""Amount""", filter.amount)
                .FilterA(@"o.""StakingUpdatesCount""", filter.stakingUpdatesCount)
                .Take(pagination, x => (@"o.""Id""", @"o.""Id"""), @"o.""Id""");

            await using var db = await DataSource.OpenConnectionAsync();
            return await db.QueryAsync(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<AutostakingOperation>> GetAutostakingOps(AutostakingOperationFilter filter, Pagination pagination, Symbols quote)
        {
            var rows = await QueryAutostakingOps(filter, pagination);
            return rows.Select(row => new AutostakingOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = Times[row.Level],
                Baker = Accounts.GetAlias(row.BakerId),
                Action = StakingActions.ToString(row.Action),
                Amount = row.Amount,
                StakingUpdatesCount = row.StakingUpdatesCount,
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<object?[][]> GetAutostakingOps(AutostakingOperationFilter filter, Pagination pagination, List<SelectionField> fields, Symbols quote)
        {
            var rows = await QueryAutostakingOps(filter, pagination, fields);

            var result = new object?[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object?[fields.Count];

            for (int i = 0, j = 0; i < fields.Count; j = 0, i++)
            {
                switch (fields[i].Full)
                {
                    case "type":
                        foreach (var row in rows)
                            result[j++][i] = ActivityTypes.Autostaking;
                        break;
                    case "id":
                        foreach (var row in rows)
                            result[j++][i] = row.Id;
                        break;
                    case "level":
                        foreach (var row in rows)
                            result[j++][i] = row.Level;
                        break;
                    case "timestamp":
                        foreach (var row in rows)
                            result[j++][i] = Times[row.Level];
                        break;
                    case "baker":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.BakerId);
                        break;
                    case "baker.alias":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.BakerId).Name;
                        break;
                    case "baker.address":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.BakerId).Address;
                        break;
                    case "action":
                        foreach (var row in rows)
                            result[j++][i] = StakingActions.ToString(row.Action);
                        break;
                    case "amount":
                        foreach (var row in rows)
                            result[j++][i] = row.Amount;
                        break;
                    case "stakingUpdatesCount":
                        foreach (var row in rows)
                            result[j++][i] = row.StakingUpdatesCount;
                        break;
                    case "quote":
                        foreach (var row in rows)
                            result[j++][i] = Quotes.Get(quote, row.Level);
                        break;
                }
            }

            return result;
        }
    }
}
