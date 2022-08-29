using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Dapper;
using Netezos.Encoding;
using Tzkt.Api.Models;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Repositories
{
    public class ContractEventsRepository : DbConnection
    {
        readonly AccountsCache Accounts;
        readonly TimeCache Times;

        public ContractEventsRepository(AccountsCache accounts, TimeCache times, IConfiguration config) : base(config)
        {
            Accounts = accounts;
            Times = times;
        }

        async Task<IEnumerable<dynamic>> QueryContractEventsAsync(ContractEventFilter filter, Pagination pagination, List<SelectionField> fields = null)
        {
            var select = @"
                ""Id"",
                ""Level"",
                ""ContractId"",
                ""ContractCodeHash"",
                ""Tag"",
                ""JsonPayload"",
                ""TransactionId""";
            if (fields != null)
            {
                var counter = 0;
                var columns = new HashSet<string>(fields.Count);
                foreach (var field in fields)
                {
                    switch (field.Field)
                    {
                        case "id": columns.Add(@"""Id"""); break;
                        case "level": columns.Add(@"""Level"""); break;
                        case "timestamp": columns.Add(@"""Level"""); break;
                        case "contract": columns.Add(@"""ContractId"""); break;
                        case "codeHash": columns.Add(@"""ContractCodeHash"""); break;
                        case "tag": columns.Add(@"""Tag"""); break;
                        case "payload":
                            if (field.Path == null)
                            {
                                columns.Add(@"""JsonPayload""");
                            }
                            else
                            {
                                field.Column = $"c{counter++}";
                                columns.Add($@"""JsonPayload"" #> '{{{field.PathString}}}' as {field.Column}");
                            }
                            break;
                        case "transactionId": columns.Add(@"""TransactionId"""); break;
                        case "type": columns.Add(@"""Type"""); break;
                        case "rawPayload": columns.Add(@"""RawPayload"""); break;
                    }
                }

                if (columns.Count == 0)
                    return Enumerable.Empty<dynamic>();

                select = string.Join(',', columns);
            }

            static (string, string) TryMetaSort(string field)
            {
                if (Regex.IsMatch(field, @"^payload(\.[\w]+)+$"))
                {
                    var col = $@"""JsonPayload""#>'{{{field[8..].Replace('.', ',')}}}'";
                    return (col, col);
                }
                return (@"""Id""", @"""Id""");
            }

            var sql = new SqlBuilder($@"
                SELECT {select} FROM ""Events""")
                .Filter("Id", filter.id)
                .Filter("Level", filter.level)
                .Filter("Level", filter.timestamp)
                .Filter("ContractId", filter.contract)
                .Filter("ContractCodeHash", filter.codeHash)
                .Filter("Tag", filter.tag)
                .Filter("JsonPayload", filter.payload)
                .Filter("TransactionId", filter.transactionId)
                .Take(pagination, x => x switch
                {
                    "id" => (@"""Id""", @"""Id"""),
                    "level" => (@"""Level""", @"""Level"""),
                    "payload" => (@"""JsonPayload""", @"""JsonPayload"""),
                    _ => TryMetaSort(x)
                }, @"""Id""");

            using var db = GetConnection();
            return await db.QueryAsync(sql.Query, sql.Params);
        }

        public async Task<int> GetContractEventsCount(ContractEventFilter filter)
        {
            var sql = new SqlBuilder(@"
                SELECT COUNT(*) FROM ""Events""")
                .Filter("Id", filter.id)
                .Filter("Level", filter.level)
                .Filter("Level", filter.timestamp)
                .Filter("ContractId", filter.contract)
                .Filter("ContractCodeHash", filter.codeHash)
                .Filter("Tag", filter.tag)
                .Filter("JsonPayload", filter.payload)
                .Filter("TransactionId", filter.transactionId);

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<ContractEvent>> GetContractEvents(ContractEventFilter filter, Pagination pagination)
        {
            var rows = await QueryContractEventsAsync(filter, pagination);
            return rows.Select(row => new ContractEvent
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = Times[row.Level],
                Contract = Accounts.GetAlias(row.ContractId),
                CodeHash = row.ContractCodeHash,
                Tag = row.Tag,
                Payload = (RawJson)row.JsonPayload,
                TransactionId = row.TransactionId,
            });
        }

        public async Task<object[][]> GetContractEvents(ContractEventFilter filter, Pagination pagination, List<SelectionField> fields)
        {
            var rows = await QueryContractEventsAsync(filter, pagination, fields);

            var result = new object[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object[fields.Count];

            for (int i = 0, j = 0; i < fields.Count; j = 0, i++)
            {
                switch (fields[i].Full)
                {
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
                    case "contract":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.ContractId);
                        break;
                    case "contract.alias":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.ContractId).Name;
                        break;
                    case "contract.address":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.ContractId).Address;
                        break;
                    case "codeHash":
                        foreach (var row in rows)
                            result[j++][i] = row.ContractCodeHash;
                        break;
                    case "tag":
                        foreach (var row in rows)
                            result[j++][i] = row.Tag;
                        break;
                    case "payload":
                        foreach (var row in rows)
                            result[j++][i] = (RawJson)row.JsonPayload;
                        break;
                    case "transactionId":
                        foreach (var row in rows)
                            result[j++][i] = row.TransactionId;
                        break;
                    case "type":
                        foreach (var row in rows)
                            result[j++][i] = Micheline.FromBytes(row.Type);
                        break;
                    case "rawPayload":
                        foreach (var row in rows)
                            result[j++][i] = Micheline.FromBytes(row.RawPayload);
                        break;
                    default:
                        if (fields[i].Field == "payload")
                            foreach (var row in rows)
                                result[j++][i] = (RawJson)((row as IDictionary<string, object>)[fields[i].Column] as string);
                        break;
                }
            }

            return result;
        }
    }
}
