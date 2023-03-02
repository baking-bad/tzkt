using Dapper;
using Tzkt.Api.Models;
using Tzkt.Data;

namespace Tzkt.Api.Repositories
{
    public partial class OperationRepository : DbConnection
    {
        public async Task<bool?> GetSmartRollupRefuteStatus(string hash)
        {
            using var db = GetConnection();
            return await GetStatus(db, nameof(TzktContext.SmartRollupRefuteOps), hash);
        }

        public async Task<int> GetSmartRollupRefuteOpsCount(SrOperationFilter filter)
        {
            var sql = new SqlBuilder(@"SELECT COUNT(*) FROM ""SmartRollupRefuteOps""")
                .Filter("Id", filter.id)
                .Filter("OpHash", filter.hash)
                .Filter("Counter", filter.counter)
                .Filter("Level", filter.level)
                .Filter("Level", filter.timestamp)
                .Filter("SenderId", filter.sender)
                .Filter("Status", filter.status)
                .Filter("SmartRollupId", filter.rollup);

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        async Task<IEnumerable<dynamic>> QuerySmartRollupRefuteOps(SrOperationFilter filter, Pagination pagination, List<SelectionField> fields = null)
        {
            var select = "*";
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
                        case "status": columns.Add(@"o.""Status"""); break;
                        case "rollup": columns.Add(@"o.""SmartRollupId"""); break;
                        case "gameId": columns.Add(@"o.""GameId"""); break;
                        case "move": columns.Add(@"o.""Move"""); break;
                        case "gameStatus": columns.Add(@"o.""GameStatus"""); break;
                        case "errors": columns.Add(@"o.""Errors"""); break;
                        case "quote": columns.Add(@"o.""Level"""); break;
                    }
                }

                if (columns.Count == 0)
                    return Enumerable.Empty<dynamic>();

                select = string.Join(',', columns);
            }

            var sql = new SqlBuilder($@"SELECT {select} FROM ""SmartRollupRefuteOps"" as o")
                .Filter("Id", filter.id)
                .Filter("OpHash", filter.hash)
                .Filter("Counter", filter.counter)
                .Filter("Level", filter.level)
                .Filter("Level", filter.timestamp)
                .Filter("SenderId", filter.sender)
                .Filter("Status", filter.status)
                .Filter("SmartRollupId", filter.rollup)
                .Take(pagination, x => (@"""Id""", @"""Id"""));

            using var db = GetConnection();
            return await db.QueryAsync(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<SmartRollupRefuteOperation>> GetSmartRollupRefuteOps(SrOperationFilter filter, Pagination pagination, Symbols quote)
        {
            var rows = await QuerySmartRollupRefuteOps(filter, pagination);
            return rows.Select(row => new SmartRollupRefuteOperation
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
                Status = OpStatuses.ToString(row.Status),
                Rollup = Accounts.GetAlias(row.SmartRollupId),
                GameId = row.GameId,
                Move = RefutationMoves.ToString((int)row.Move),
                GameStatus = RefutationGameStatuses.ToString((int)row.GameStatus),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<object[][]> GetSmartRollupRefuteOps(SrOperationFilter filter, Pagination pagination, List<SelectionField> fields, Symbols quote)
        {
            var rows = await QuerySmartRollupRefuteOps(filter, pagination, fields);

            var result = new object[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object[fields.Count];

            for (int i = 0, j = 0; i < fields.Count; j = 0, i++)
            {
                switch (fields[i].Full)
                {
                    case "type":
                        foreach (var row in rows)
                            result[j++][i] = OpTypes.SmartRollupRefute;
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
                    case "status":
                        foreach (var row in rows)
                            result[j++][i] = OpStatuses.ToString(row.Status);
                        break;
                    case "rollup":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.SmartRollupId);
                        break;
                    case "rollup.alias":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.SmartRollupId).Name;
                        break;
                    case "rollup.address":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.SmartRollupId).Address;
                        break;
                    case "gameId":
                        foreach (var row in rows)
                            result[j++][i] = row.GameId;
                        break;
                    case "move":
                        foreach (var row in rows)
                            result[j++][i] = RefutationMoves.ToString((int)row.Move);
                        break;
                    case "gameStatus":
                        foreach (var row in rows)
                            result[j++][i] = RefutationGameStatuses.ToString((int)row.GameStatus);
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
