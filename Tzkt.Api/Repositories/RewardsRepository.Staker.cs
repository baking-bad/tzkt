using Dapper;
using Tzkt.Api.Models;

namespace Tzkt.Api.Repositories
{
    public partial class RewardsRepository
    {
        const string FinalStakeExpr = """
                COALESCE(sc."FinalStake", FLOOR(baker."ExternalStakedBalance" * COALESCE(staker."StakedPseudotokens", 0::numeric) / COALESCE(baker."IssuedPseudotokens", 1::numeric))::bigint)
                """;

        const string FinalStakeColumn = $"""
                {FinalStakeExpr} AS "_FinalStake"
                """;

        const string RewardsColumn = $"""
                ({FinalStakeExpr} + sc."RemovedStake" - sc."AddedStake" - sc."InitialStake") AS "_Rewards"
                """;

        async Task<IEnumerable<dynamic>> QueryStakerRewardsAsync(StakerRewardsFilter filter, Pagination pagination, List<SelectionField>? fields = null)
        {
            var select = $"sc.*, bc.*, {FinalStakeColumn}, {RewardsColumn}";
            if (fields != null)
            {
                var columns = new HashSet<string>(fields.Count);
                foreach (var field in fields)
                {
                    switch (field.Field)
                    {
                        case "cycle": columns.Add(@"sc.""Cycle"""); break;
                        case "bakerId": columns.Add(@"sc.""BakerId"""); break;
                        case "initialStake": columns.Add(@"sc.""InitialStake"""); break;
                        case "addedStake": columns.Add(@"sc.""AddedStake"""); break;
                        case "removedStake": columns.Add(@"sc.""RemovedStake"""); break;
                        case "finalStake": columns.Add(FinalStakeColumn); break;
                        case "avgStake": columns.Add(@"sc.""AvgStake"""); break;
                        case "rewards": columns.Add(RewardsColumn); break;
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
                        case "quote": columns.Add(@"sc.""Cycle"""); break;
                    }
                }

                if (columns.Count == 0)
                    return [];

                select = string.Join(',', columns);
            }

            var sql = new SqlBuilder($"""
                SELECT {select} FROM "StakerCycles" AS sc
                INNER JOIN "BakerCycles" AS bc ON bc."Cycle" = sc."Cycle" AND bc."BakerId" = sc."BakerId"
                INNER JOIN "Accounts" AS baker ON baker."Id" = sc."BakerId"
                INNER JOIN "Accounts" AS staker ON staker."Id" = sc."StakerId"
                """)
                .FilterA(@"sc.""Cycle""", filter.cycle)
                .FilterA(@"sc.""BakerId""", filter.baker)
                .FilterA(@"sc.""StakerId""", filter.staker)
                .Take(pagination, x => x switch
                {
                    "cycle" => (@"sc.""Cycle""", @"sc.""Cycle"""),
                    _ => (@"sc.""Id""", @"sc.""Id""")
                }, @"sc.""Id""");

            await using var db = await dataSource.OpenConnectionAsync();
            return await db.QueryAsync(sql.Query, sql.Params);
        }

        public async Task<int> GetStakerRewardsCount(StakerRewardsFilter filter)
        {
            var sql = new SqlBuilder("""
                SELECT COUNT(*) FROM "StakerCycles" AS sc
                """)
                .FilterA(@"sc.""Cycle""", filter.cycle)
                .FilterA(@"sc.""BakerId""", filter.baker)
                .FilterA(@"sc.""StakerId""", filter.staker);

            await using var db = await dataSource.OpenConnectionAsync();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<StakerRewards>> GetStakerRewards(StakerRewardsFilter filter, Pagination pagination, Symbols quote)
        {
            var rows = await QueryStakerRewardsAsync(filter, pagination);
            return rows.Select(row => new StakerRewards
            {
                Cycle = row.Cycle,
                Baker = accounts.GetAlias((int)row.BakerId),
                InitialStake = row.InitialStake,
                AddedStake = row.AddedStake,
                RemovedStake = row.RemovedStake,
                FinalStake = row._FinalStake,
                AvgStake = row.AvgStake,
                Rewards = row._Rewards,
                BakerRewards = ExtractBakerRewards(row, quote),
                Quote = quotes.Get(quote, protocols.FindByCycle((int)row.Cycle).GetCycleEnd((int)row.Cycle))
            });
        }

        public async Task<object?[][]> GetStakerRewards(StakerRewardsFilter filter, Pagination pagination, List<SelectionField> fields, Symbols quote)
        {
            var rows = await QueryStakerRewardsAsync(filter, pagination, fields);

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
                    case "initialStake":
                        foreach (var row in rows)
                            result[j++][i] = row.InitialStake;
                        break;
                    case "addedStake":
                        foreach (var row in rows)
                            result[j++][i] = row.AddedStake;
                        break;
                    case "removedStake":
                        foreach (var row in rows)
                            result[j++][i] = row.RemovedStake;
                        break;
                    case "finalStake":
                        foreach (var row in rows)
                            result[j++][i] = row._FinalStake;
                        break;
                    case "avgStake":
                        foreach (var row in rows)
                            result[j++][i] = row.AvgStake;
                        break;
                    case "rewards":
                        foreach (var row in rows)
                            result[j++][i] = row._Rewards;
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
