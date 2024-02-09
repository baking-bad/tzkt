using Dapper;
using Mvkt.Api.Models;

namespace Mvkt.Api.Repositories
{
    public partial class OperationRepository
    {
        public async Task<int> GetSetDelegateParametersOpsCount(ManagerOperationFilter filter)
        {
            var sql = new SqlBuilder("""
                SELECT COUNT(*)
                FROM "SetDelegateParametersOps" as o
                """)
                .FilterA(@"o.""Id""", filter.id)
                .FilterA(@"o.""OpHash""", filter.hash)
                .FilterA(@"o.""Counter""", filter.counter)
                .FilterA(@"o.""Level""", filter.level)
                .FilterA(@"o.""Level""", filter.timestamp)
                .FilterA(@"o.""SenderId""", filter.sender)
                .FilterA(@"o.""Status""", filter.status);

            await using var db = await DataSource.OpenConnectionAsync();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        async Task<IEnumerable<dynamic>> QuerySetDelegateParametersOps(ManagerOperationFilter filter, Pagination pagination, List<SelectionField> fields = null)
        {
            var select = "o.*";

            if (fields != null)
            {
                var columns = new HashSet<string>(fields.Count);
                foreach (var field in fields)
                {
                    switch (field.Field)
                    {
                        case "id": columns.Add(@"o.""Id"""); break;
                        case "level": columns.Add(@"o.""Level"""); break;
                        case "timestamp": columns.Add(@"o.""Timestamp"""); break;
                        case "hash": columns.Add(@"o.""OpHash"""); break;
                        case "sender": columns.Add(@"o.""SenderId"""); break;
                        case "counter": columns.Add(@"o.""Counter"""); break;
                        case "gasLimit": columns.Add(@"o.""GasLimit"""); break;
                        case "gasUsed": columns.Add(@"o.""GasUsed"""); break;
                        case "storageLimit": columns.Add(@"o.""StorageLimit"""); break;
                        case "bakerFee": columns.Add(@"o.""BakerFee"""); break;
                        case "limitOfStakingOverBaking": columns.Add(@"o.""LimitOfStakingOverBaking"""); break;
                        case "edgeOfBakingOverStaking": columns.Add(@"o.""EdgeOfBakingOverStaking"""); break;
                        case "activationCycle": columns.Add(@"o.""ActivationCycle"""); break;
                        case "status": columns.Add(@"o.""Status"""); break;
                        case "errors": columns.Add(@"o.""Errors"""); break;
                        case "quote": columns.Add(@"o.""Level"""); break;
                    }
                }

                if (columns.Count == 0)
                    return Enumerable.Empty<dynamic>();

                select = string.Join(',', columns);
            }

            var sql = new SqlBuilder($"""
                SELECT {select}
                FROM "SetDelegateParametersOps" as o
                """)
                .FilterA(@"o.""Id""", filter.id)
                .FilterA(@"o.""OpHash""", filter.hash)
                .FilterA(@"o.""Counter""", filter.counter)
                .FilterA(@"o.""Level""", filter.level)
                .FilterA(@"o.""Level""", filter.timestamp)
                .FilterA(@"o.""SenderId""", filter.sender)
                .FilterA(@"o.""Status""", filter.status)
                .Take(pagination, x => (@"o.""Id""", @"o.""Id"""), @"o.""Id""");

            await using var db = await DataSource.OpenConnectionAsync();
            return await db.QueryAsync(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<SetDelegateParametersOperation>> GetSetDelegateParametersOps(ManagerOperationFilter filter, Pagination pagination, Symbols quote)
        {
            var rows = await QuerySetDelegateParametersOps(filter, pagination);
            return rows.Select(row => new SetDelegateParametersOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Sender = Accounts.GetAlias(row.SenderId),
                Counter = row.Counter,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                StorageLimit = row.StorageLimit,
                BakerFee = row.BakerFee,
                LimitOfStakingOverBaking = row.LimitOfStakingOverBaking,
                EdgeOfBakingOverStaking = row.EdgeOfBakingOverStaking,
                ActivationCycle = row.ActivationCycle,
                Status = OpStatuses.ToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<object[][]> GetSetDelegateParametersOps(ManagerOperationFilter filter, Pagination pagination, List<SelectionField> fields, Symbols quote)
        {
            var rows = await QuerySetDelegateParametersOps(filter, pagination, fields);

            var result = new object[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object[fields.Count];

            for (int i = 0, j = 0; i < fields.Count; j = 0, i++)
            {
                switch (fields[i].Full)
                {
                    case "type":
                        foreach (var row in rows)
                            result[j++][i] = OpTypes.SetDelegateParameters;
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
                            result[j++][i] = row.Timestamp;
                        break;
                    case "hash":
                        foreach (var row in rows)
                            result[j++][i] = row.OpHash;
                        break;
                    case "sender":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.SenderId);
                        break;
                    case "sender.alias":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.SenderId).Name;
                        break;
                    case "sender.address":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.SenderId).Address;
                        break;
                    case "counter":
                        foreach (var row in rows)
                            result[j++][i] = row.Counter;
                        break;
                    case "gasLimit":
                        foreach (var row in rows)
                            result[j++][i] = row.GasLimit;
                        break;
                    case "gasUsed":
                        foreach (var row in rows)
                            result[j++][i] = row.GasUsed;
                        break;
                    case "storageLimit":
                        foreach (var row in rows)
                            result[j++][i] = row.StorageLimit;
                        break;
                    case "bakerFee":
                        foreach (var row in rows)
                            result[j++][i] = row.BakerFee;
                        break;
                    case "limitOfStakingOverBaking":
                        foreach (var row in rows)
                            result[j++][i] = row.LimitOfStakingOverBaking;
                        break;
                    case "edgeOfBakingOverStaking":
                        foreach (var row in rows)
                            result[j++][i] = row.EdgeOfBakingOverStaking;
                        break;
                    case "activationCycle":
                        foreach (var row in rows)
                            result[j++][i] = row.ActivationCycle;
                        break;
                    case "status":
                        foreach (var row in rows)
                            result[j++][i] = OpStatuses.ToString(row.Status);
                        break;
                    case "errors":
                        foreach (var row in rows)
                            result[j++][i] = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null;
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
