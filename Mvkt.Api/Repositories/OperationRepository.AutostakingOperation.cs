using Dapper;
using Mvkt.Api.Models;

namespace Mvkt.Api.Repositories
{
    public partial class OperationRepository : DbConnection
    {
        public async Task<int> GetAutostakingOpsCount(AutostakingOperationFilter filter)
        {
            var sql = new SqlBuilder("""
                SELECT COUNT(*)
                FROM "AutostakingOps" as o
                """)
                .FilterA(@"o.""Id""", filter.id)
                .FilterA(@"o.""Level""", filter.level)
                .FilterA(@"o.""Level""", filter.timestamp)
                .FilterA(@"o.""BakerId""", filter.baker)
                .FilterA(@"o.""Action""", filter.action)
                .FilterA(@"o.""Cycle""", filter.cycle)
                .FilterA(@"o.""Amount""", filter.amount);

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        async Task<IEnumerable<dynamic>> QueryAutostakingOps(AutostakingOperationFilter filter, Pagination pagination, List<SelectionField> fields = null)
        {
            var select = """
                o."Id",
                o."Level",
                o."BakerId",
                o."Action",
                o."Cycle",
                o."Amount"
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
                        case "cycle": columns.Add(@"o.""Cycle"""); break;
                        case "amount": columns.Add(@"o.""Amount"""); break;
                        case "quote": columns.Add(@"o.""Level"""); break;
                    }
                }

                if (columns.Count == 0)
                    return Enumerable.Empty<dynamic>();

                select = string.Join(',', columns);
            }

            var sql = new SqlBuilder($"""
                SELECT {select}
                FROM "AutostakingOps" as o
                """)
                .FilterA(@"o.""Id""", filter.id)
                .FilterA(@"o.""Level""", filter.level)
                .FilterA(@"o.""Level""", filter.timestamp)
                .FilterA(@"o.""BakerId""", filter.baker)
                .FilterA(@"o.""Action""", filter.action)
                .FilterA(@"o.""Cycle""", filter.cycle)
                .FilterA(@"o.""Amount""", filter.amount)
                .Take(pagination, x => (@"o.""Id""", @"o.""Id"""), @"o.""Id""");

            using var db = GetConnection();
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
                Action = AutostakingActions.ToString(row.Action),
                Cycle = row.Cycle,
                Amount = row.Amount,
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<object[][]> GetAutostakingOps(AutostakingOperationFilter filter, Pagination pagination, List<SelectionField> fields, Symbols quote)
        {
            var rows = await QueryAutostakingOps(filter, pagination, fields);

            var result = new object[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object[fields.Count];

            for (int i = 0, j = 0; i < fields.Count; j = 0, i++)
            {
                switch (fields[i].Full)
                {
                    case "type":
                        foreach (var row in rows)
                            result[j++][i] = OpTypes.Autostaking;
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
                            result[j++][i] = AutostakingActions.ToString(row.Action);
                        break;
                    case "cycle":
                        foreach (var row in rows)
                            result[j++][i] = row.Cycle;
                        break;
                    case "amount":
                        foreach (var row in rows)
                            result[j++][i] = row.Amount;
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
