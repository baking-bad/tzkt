using Dapper;
using Tzkt.Api.Models;

namespace Tzkt.Api.Repositories
{
    public partial class RewardsRepository
    {
        async Task<IEnumerable<dynamic>> QueryDelegatorRewardsAsync(DelegatorRewardsFilter filter, Pagination pagination, List<SelectionField>? fields = null)
        {
            const string stakedBalanceCol = """
                (bc."ExternalStakedBalance"::numeric * dc."StakedPseudotokens" / bc."IssuedPseudotokens")::bigint as "StakedBalance"
                """;

            var select = $"""
                dc.*,
                bc.*,
                {stakedBalanceCol}
                """;
                
            if (fields != null)
            {
                var columns = new HashSet<string>(fields.Count);
                foreach (var field in fields)
                {
                    switch (field.Field)
                    {
                        case "cycle": columns.Add(@"dc.""Cycle"""); break;
                        case "baker": columns.Add(@"dc.""BakerId"""); break;
                        case "delegatedBalance": columns.Add(@"dc.""DelegatedBalance"""); break;
                        case "stakedPseudotokens": columns.Add(@"dc.""StakedPseudotokens"""); break;
                        case "stakedBalance": columns.Add(stakedBalanceCol); break;
                        case "bakerRewards":
                            if (field.SubField() is SelectionField subField)
                            {
                                ProcessBakerRewardsField(subField, columns);
                            }
                            else
                            {
                                columns.Add("bc.*");
                            }
                            break;
                        case "quote": columns.Add(@"dc.""Cycle"""); break;
                    }
                }

                if (columns.Count == 0)
                    return [];

                select = string.Join(',', columns);
            }

            var sql = new SqlBuilder($"""
                SELECT {select} FROM "DelegatorCycles" AS dc
                INNER JOIN "BakerCycles" AS bc ON bc."Cycle" = dc."Cycle" AND bc."BakerId" = dc."BakerId"
                """)
                .FilterA(@"dc.""Cycle""", filter.cycle)
                .FilterA(@"dc.""BakerId""", filter.baker)
                .FilterA(@"dc.""DelegatorId""", filter.delegator)
                .Take(pagination, x => x switch
                {
                    "cycle" => (@"dc.""Cycle""", @"dc.""Cycle"""),
                    _ => (@"dc.""Id""", @"dc.""Id""")
                }, @"dc.""Id""");

            await using var db = await dataSource.OpenConnectionAsync();
            return await db.QueryAsync(sql.Query, sql.Params);
        }

        public async Task<int> GetDelegatorRewardsCount(DelegatorRewardsFilter filter)
        {
            var sql = new SqlBuilder("""
                SELECT COUNT(*) FROM "DelegatorCycles" AS dc
                """)
                .FilterA(@"dc.""Cycle""", filter.cycle)
                .FilterA(@"dc.""BakerId""", filter.baker)
                .FilterA(@"dc.""DelegatorId""", filter.delegator);

            await using var db = await dataSource.OpenConnectionAsync();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<DelegatorRewards>> GetDelegatorRewards(DelegatorRewardsFilter filter, Pagination pagination, Symbols quote)
        {
            var rows = await QueryDelegatorRewardsAsync(filter, pagination);
            return rows.Select(row => new DelegatorRewards
            {
                Cycle = row.Cycle,
                Baker = accounts.GetAlias(row.BakerId),
                DelegatedBalance = row.DelegatedBalance,
                StakedPseudotokens = row.StakedPseudotokens,
                StakedBalance = row.StakedBalance,
                BakerRewards = ExtractBakerRewards(row, quote),
                Quote = quotes.Get(quote, protocols.FindByCycle((int)row.Cycle).GetCycleEnd((int)row.Cycle))
            });
        }

        public async Task<object?[][]> GetDelegatorRewards(DelegatorRewardsFilter filter, Pagination pagination, List<SelectionField> fields, Symbols quote)
        {
            var rows = await QueryDelegatorRewardsAsync(filter, pagination, fields);

            var result = new object?[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object?[fields.Count];

            for (int i = 0, j = 0; i < fields.Count; j = 0, i++)
            {
                switch (fields[i].Full)
                {
                    case "cycle":
                        foreach (var row in rows)
                            result[j++][i] = row.Cycle;
                        break;
                    case "baker":
                        foreach (var row in rows)
                            result[j++][i] = accounts.GetAlias((int)row.BakerId);
                        break;
                    case "baker.alias":
                        foreach (var row in rows)
                            result[j++][i] = accounts.GetAlias((int)row.BakerId).Name;
                        break;
                    case "baker.address":
                        foreach (var row in rows)
                            result[j++][i] = accounts.GetAlias((int)row.BakerId).Address;
                        break;
                    case "delegatedBalance":
                        foreach (var row in rows)
                            result[j++][i] = row.DelegatedBalance;
                        break;
                    case "stakedPseudotokens":
                        foreach (var row in rows)
                            result[j++][i] = row.StakedPseudotokens;
                        break;
                    case "stakedBalance":
                        foreach (var row in rows)
                            result[j++][i] = row.StakedBalance;
                        break;
                    case "bakerRewards":
                        foreach (var row in rows)
                            result[j++][i] = ExtractBakerRewards(row, quote);
                        break;
                    case "quote":
                        foreach (var row in rows)
                            result[j++][i] = quotes.Get(quote, protocols.FindByCycle((int)row.Cycle).GetCycleEnd((int)row.Cycle));
                        break;
                    default:
                        if (fields[i].Field == "bakerRewards" && fields[i].SubField() is SelectionField subField)
                        {
                            WriteBakerRewardsField(subField, rows, i, result, quote);
                        }
                        break;
                }
            }

            return result;
        }
    }
}
