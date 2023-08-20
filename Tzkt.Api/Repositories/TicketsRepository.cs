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
    public class TicketsRepository : DbConnection
    {
        readonly AccountsCache Accounts;
        readonly TimeCache Times;

        public TicketsRepository(AccountsCache accounts, TimeCache times, IConfiguration config) : base(config)
        {
            Accounts = accounts;
            Times = times;
        }

        #region tickets
        async Task<IEnumerable<dynamic>> QueryTicketsAsync(TicketFilter filter, Pagination pagination, MichelineFormat format, List<SelectionField> fields = null)
        {
            var select = "*";
            if (fields != null)
            {
                var counter = 0;
                var columns = new HashSet<string>(fields.Count);
                foreach (var field in fields)
                {
                    switch (field.Field)
                    {
                        case "id": columns.Add(@"""Id"""); break;
                        case "ticketer": columns.Add(@"""TicketerId"""); break;
                        case "firstMinter": columns.Add(@"""FirstMinterId"""); break;
                        case "firstLevel": columns.Add(@"""FirstLevel"""); break;
                        case "firstTime": columns.Add(@"""FirstLevel"""); break;
                        case "lastLevel": columns.Add(@"""LastLevel"""); break;
                        case "lastTime": columns.Add(@"""LastLevel"""); break;
                        case "transfersCount": columns.Add(@"""TransfersCount"""); break;
                        case "balancesCount": columns.Add(@"""BalancesCount"""); break;
                        case "holdersCount": columns.Add(@"""HoldersCount"""); break;
                        case "totalMinted": columns.Add(@"""TotalMinted"""); break;
                        case "totalBurned": columns.Add(@"""TotalBurned"""); break;
                        case "totalSupply": columns.Add(@"""TotalSupply"""); break;
                        case "contentHash": columns.Add(@"o.""ContentHash"""); break;
                        case "contentTypeHash": columns.Add(@"o.""ContentTypeHash"""); break;
                        case "contentType":
                            if (field.Path == null)
                            {
                                columns.Add(format <= MichelineFormat.JsonString ? @"""JsonType""" : @"""RawType""");
                            }
                            else if (format <= MichelineFormat.JsonString)
                            {
                                field.Column = $"c{counter++}";
                                columns.Add($@"""JsonType"" #> '{{{field.PathString}}}' as {field.Column}");
                            }
                            else
                            {
                                field.Column = $"c{counter++}";
                                columns.Add($@"NULL as {field.Column}");
                            }
                            break;
                        case "content":
                            if (field.Path == null)
                            {
                                columns.Add(format <= MichelineFormat.JsonString ? @"""JsonContent""" : @"""RawContent""");
                            }
                            else if (format <= MichelineFormat.JsonString)
                            {
                                field.Column = $"c{counter++}";
                                columns.Add($@"""JsonContent"" #> '{{{field.PathString}}}' as {field.Column}");
                            }
                            else
                            {
                                field.Column = $"c{counter++}";
                                columns.Add($@"NULL as {field.Column}");
                            }
                            break;
                    }
                }

                if (columns.Count == 0)
                    return Enumerable.Empty<dynamic>();

                select = string.Join(',', columns);
            }

            var sql = new SqlBuilder($@"SELECT {select} FROM ""Tickets""")
                .Filter("Id", filter.id)
                .Filter("TicketerId", filter.ticketer)
                .Filter("FirstMinterId", filter.firstMinter)
                .Filter("FirstLevel", filter.firstLevel)
                .Filter("FirstLevel", filter.firstTime)
                .Filter("LastLevel", filter.lastLevel)
                .Filter("LastLevel", filter.lastTime)
                .Filter("ContentTypeHash", filter.contentTypeHash)
                .Filter("ContentHash", filter.contentHash)
                .Filter("JsonType", filter.contentType)
                .Filter("JsonContent", filter.content)
                .Take(pagination, x => x switch
                {
                    "ticketId" => (@"""TicketId""::numeric", @"""TicketId""::numeric"),
                    "transfersCount" => (@"""TransfersCount""", @"""TransfersCount"""),
                    "holdersCount" => (@"""HoldersCount""", @"""HoldersCount"""),
                    "balancesCount" => (@"""BalancesCount""", @"""BalancesCount"""),
                    "firstLevel" => (@"""Id""", @"""FirstLevel"""),
                    "lastLevel" => (@"""LastLevel""", @"""LastLevel"""),
                    "metadata" => (@"""Metadata""", @"""Metadata"""),
                    "content" => (@"""JsonContent""", @"""JsonContent"""),
                    "contentType" => (@"""JsonType""", @"""JsonType"""),
                    _ => (@"""Id""", @"""Id""")
                });

            using var db = GetConnection();
            return await db.QueryAsync(sql.Query, sql.Params);
        }

        public async Task<int> GetTicketsCount(TicketFilter filter)
        {
            var sql = new SqlBuilder(@"SELECT COUNT(*) FROM ""Tickets""")
                .Filter("Id", filter.id)
                .Filter("TicketerId", filter.ticketer)
                .Filter("FirstMinterId", filter.firstMinter)
                .Filter("FirstLevel", filter.firstLevel)
                .Filter("FirstLevel", filter.firstTime)
                .Filter("LastLevel", filter.lastLevel)
                .Filter("LastLevel", filter.lastTime)
                .Filter("ContentTypeHash", filter.contentTypeHash)
                .Filter("ContentHash", filter.contentHash)
                .Filter("JsonType", filter.contentType)
                .Filter("JsonContent", filter.content);

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<Ticket>> GetTickets(TicketFilter filter, Pagination pagination, MichelineFormat format)
        {
            var rows = await QueryTicketsAsync(filter, pagination, format);
            return rows.Select(row => new Ticket
            {
                Ticketer = Accounts.GetAlias(row.TicketerId),
                Id = row.Id,
                BalancesCount = row.BalancesCount,
                FirstMinter = Accounts.GetAlias(row.FirstMinterId),
                FirstLevel = row.FirstLevel,
                FirstTime = Times[row.FirstLevel],
                HoldersCount = row.HoldersCount,
                LastLevel = row.LastLevel,
                LastTime = Times[row.LastLevel],
                TotalBurned = row.TotalBurned,
                TotalMinted = row.TotalMinted,
                TotalSupply = row.TotalSupply,
                TransfersCount = row.TransfersCount,
                ContentType = (RawJson)Micheline.ToJson(row.RawType),
                Content = format switch
                {
                    MichelineFormat.Json => row.JsonContent == null ? null : (RawJson)row.JsonContent,
                    MichelineFormat.JsonString => row.JsonContent,
                    MichelineFormat.Raw => row.RawContent == null ? null : (RawJson)Micheline.ToJson(row.RawContent),
                    MichelineFormat.RawString => row.RawContent == null ? null : Micheline.ToJson(row.RawContent),
                    _ => throw new Exception("Invalid MichelineFormat value")
                },
                ContentHash = row.ContentHash,
                ContentTypeHash = row.ContentTypeHash
            });
        }

        public async Task<object[][]> GetTickets(TicketFilter filter, Pagination pagination, MichelineFormat format, List<SelectionField> fields)
        {
            var rows = await QueryTicketsAsync(filter, pagination, format, fields);

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
                    case "ticketer":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.TicketerId);
                        break;
                    case "ticketer.alias":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.TicketerId).Name;
                        break;
                    case "ticketer.address":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.TicketerId).Address;
                        break;
                    case "contentType":
                        foreach (var row in rows)
                            result[j++][i] = format switch
                            {
                                MichelineFormat.Json => row.JsonType == null ? null : (RawJson)row.JsonType,
                                MichelineFormat.JsonString => row.JsonType,
                                MichelineFormat.Raw => row.RawType == null ? null : (RawJson)Micheline.ToJson(row.RawType),
                                MichelineFormat.RawString => row.RawType == null ? null : Micheline.ToJson(row.RawType),
                                _ => throw new Exception("Invalid MichelineFormat value")
                            };
                        break;
                    case "content":
                        foreach (var row in rows)
                            result[j++][i] = format switch
                            {
                                MichelineFormat.Json => row.JsonContent == null ? null : (RawJson)row.JsonContent,
                                MichelineFormat.JsonString => row.JsonContent,
                                MichelineFormat.Raw => row.RawContent == null ? null : (RawJson)Micheline.ToJson(row.RawContent),
                                MichelineFormat.RawString => row.RawContent == null ? null : Micheline.ToJson(row.RawContent),
                                _ => throw new Exception("Invalid MichelineFormat value")
                            };
                        break;
                    case "contentTypeHash":
                        foreach (var row in rows)
                            result[j++][i] = row.ContentTypeHash;
                        break;
                    case "contentHash":
                        foreach (var row in rows)
                            result[j++][i] = row.ContentType;
                        break;
                    case "firstMinter":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.FirstMinterId);
                        break;
                    case "firstMinter.alias":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.FirstMinterId).Name;
                        break;
                    case "firstMinter.address":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.FirstMinterId).Address;
                        break;
                    case "firstLevel":
                        foreach (var row in rows)
                            result[j++][i] = row.FirstLevel;
                        break;
                    case "firstTime":
                        foreach (var row in rows)
                            result[j++][i] = Times[row.FirstLevel];
                        break;
                    case "lastLevel":
                        foreach (var row in rows)
                            result[j++][i] = row.LastLevel;
                        break;
                    case "lastTime":
                        foreach (var row in rows)
                            result[j++][i] = Times[row.LastLevel];
                        break;
                    case "transfersCount":
                        foreach (var row in rows)
                            result[j++][i] = row.TransfersCount;
                        break;
                    case "balancesCount":
                        foreach (var row in rows)
                            result[j++][i] = row.BalancesCount;
                        break;
                    case "holdersCount":
                        foreach (var row in rows)
                            result[j++][i] = row.HoldersCount;
                        break;
                    case "totalMinted":
                        foreach (var row in rows)
                            result[j++][i] = row.TotalMinted;
                        break;
                    case "totalBurned":
                        foreach (var row in rows)
                            result[j++][i] = row.TotalBurned;
                        break;
                    case "totalSupply":
                        foreach (var row in rows)
                            result[j++][i] = row.TotalSupply;
                        break;
                }
            }

            return result;
        }
        #endregion

        #region ticket balances
        async Task<IEnumerable<dynamic>> QueryTicketBalancesAsync(TicketBalanceFilter filter, Pagination pagination, MichelineFormat format, List<SelectionField> fields = null)
        {
            var select = @"
                tb.""Id"",
                tb.""AccountId"",
                tb.""Balance"",
                tb.""FirstLevel"",
                tb.""LastLevel"",
                tb.""TransfersCount"",
                tb.""TicketId"" as ""tId"",
                tb.""TicketerId"" as ""tTicketerId"",
                t.""JsonContent"" as ""JsonContent"",
                t.""RawContent"" as ""RawContent"",
                t.""ContentHash"" as ""ContentHash"",
                t.""ContentTypeHash"" as ""ContentTypeHash"",
                t.""RawType"" as ""RawType"",
                t.""TotalSupply"" as ""tTotalSupply""";
            if (fields != null)
            {
                var counter = 0;
                var columns = new HashSet<string>(fields.Count);
                foreach (var field in fields)
                {
                    switch (field.Field)
                    {
                        case "id": columns.Add(@"tb.""Id"""); break;
                        case "account": columns.Add(@"tb.""AccountId"""); break;
                        case "balance": columns.Add(@"tb.""Balance"""); break;
                        case "firstLevel": columns.Add(@"tb.""FirstLevel"""); break;
                        case "firstTime": columns.Add(@"tb.""FirstLevel"""); break;
                        case "lastLevel": columns.Add(@"tb.""LastLevel"""); break;
                        case "lastTime": columns.Add(@"tb.""LastLevel"""); break;
                        case "transfersCount": columns.Add(@"tb.""TransfersCount"""); break;
                        case "ticket":
                            columns.Add(@"tb.""TicketId"" as ""tId""");
                            columns.Add(@"tb.""TicketerId"" as ""tTicketerId""");
                            columns.Add(@"t.""TotalSupply"" as ""tTotalSupply""");
                            columns.Add(format switch
                            {
                                MichelineFormat.Json => @"t.""JsonContent"" as ""JsonContent""",
                                MichelineFormat.JsonString => @"t.""JsonContent"" as ""JsonContent""",
                                MichelineFormat.Raw => @"t.""RawContent"" as ""RawContent""",
                                MichelineFormat.RawString => @"t.""RawContent"" as ""RawContent""",
                                _ => throw new Exception("Invalid MichelineFormat value")
                            });
                            columns.Add(@"t.""ContentHash"" as ""ContentHash""");
                            columns.Add(@"t.""ContentTypeHash"" as ""ContentTypeHash""");
                            columns.Add(@"t.""RawType"" as ""RawType""");
                            break;
                    }
                }

                if (columns.Count == 0)
                    return Enumerable.Empty<dynamic>();

                select = string.Join(',', columns);
            }
            
            var sql = new SqlBuilder($@"
                SELECT {select} FROM ""TicketBalances"" as tb
                INNER JOIN ""Tickets"" AS t ON t.""Id"" = tb.""TicketId""")
                .FilterA(@"tb.""Id""", filter.id)
                .FilterA(@"tb.""AccountId""", filter.account)
                .FilterA(@"tb.""Balance""", filter.balance)
                .FilterA(@"tb.""FirstLevel""", filter.firstLevel)
                .FilterA(@"tb.""FirstLevel""", filter.firstTime)
                .FilterA(@"tb.""LastLevel""", filter.lastLevel)
                .FilterA(@"tb.""LastLevel""", filter.lastTime)
                .FilterA(@"tb.""TicketId""", filter.ticket.id)
                .FilterA(@"tb.""TicketerId""", filter.ticket.ticketer)
                .FilterA(@"t.""ContentHash""", filter.ticket.contentHash)
                .FilterA(@"t.""ContentTypeHash""", filter.ticket.contentTypeHash)
                .Take(pagination, x => x switch
                {
                    "balance" => (@"tb.""Balance""::numeric", @"tb.""Balance""::numeric"),
                    "transfersCount" => (@"tb.""TransfersCount""", @"tb.""TransfersCount"""),
                    "firstLevel" => (@"tb.""Id""", @"tb.""FirstLevel"""),
                    "lastLevel" => (@"tb.""LastLevel""", @"tb.""LastLevel"""),
                    _ => (@"tb.""Id""", @"tb.""Id""")
                }, @"tb.""Id""");

            using var db = GetConnection();
            return await db.QueryAsync(sql.Query, sql.Params);
        }

        public async Task<int> GetTicketBalancesCount(TicketBalanceFilter filter)
        {
            var sql = new SqlBuilder(@"
                SELECT COUNT(*) FROM ""TicketBalances"" as tb
                INNER JOIN ""Tickets"" AS t ON t.""Id"" = tb.""TicketId""")
                .FilterA(@"tb.""Id""", filter.id)
                .FilterA(@"tb.""AccountId""", filter.account)
                .FilterA(@"tb.""Balance""", filter.balance)
                .FilterA(@"tb.""FirstLevel""", filter.firstLevel)
                .FilterA(@"tb.""FirstLevel""", filter.firstTime)
                .FilterA(@"tb.""LastLevel""", filter.lastLevel)
                .FilterA(@"tb.""LastLevel""", filter.lastTime)
                .FilterA(@"tb.""TicketId""", filter.ticket.id)
                .FilterA(@"tb.""TicketerId""", filter.ticket.ticketer)
                .FilterA(@"t.""ContentHash""", filter.ticket.contentHash)
                .FilterA(@"t.""ContentTypeHash""", filter.ticket.contentTypeHash);

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<TicketBalance>> GetTicketBalances(TicketBalanceFilter filter, Pagination pagination, MichelineFormat format)
        {
            var rows = await QueryTicketBalancesAsync(filter, pagination, format);
            return rows.Select(row => new TicketBalance
            {
                Id = row.Id,
                Account = Accounts.GetAlias(row.AccountId),
                Balance = row.Balance,
                FirstLevel = row.FirstLevel,
                FirstTime = Times[row.FirstLevel],
                LastLevel = row.LastLevel,
                LastTime = Times[row.LastLevel],
                TransfersCount = row.TransfersCount,
                Ticket = new TicketInfo
                {
                    Id = row.tId,
                    Ticketer = Accounts.GetAlias(row.tTicketerId),
                    TotalSupply = row.tTotalSupply,
                    ContentType = (RawJson)Micheline.ToJson(row.RawType),
                    Content = format switch
                    {
                        MichelineFormat.Json => row.JsonContent == null ? null : (RawJson)row.JsonContent,
                        MichelineFormat.JsonString => row.JsonContent,
                        MichelineFormat.Raw => row.RawContent == null ? null : (RawJson)Micheline.ToJson(row.RawContent),
                        MichelineFormat.RawString => row.RawContent == null ? null : Micheline.ToJson(row.RawContent),
                        _ => throw new Exception("Invalid MichelineFormat value")
                    },
                    ContentHash = row.ContentHash,
                    ContentTypeHash = row.ContentTypeHash
                }
            });
        }

        public async Task<object[][]> GetTicketBalances(TicketBalanceFilter filter, Pagination pagination, MichelineFormat format, List<SelectionField> fields)
        {
            var rows = await QueryTicketBalancesAsync(filter, pagination, format, fields);

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
                    case "account":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.AccountId);
                        break;
                    case "account.alias":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.AccountId).Name;
                        break;
                    case "account.address":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.AccountId).Address;
                        break;
                    case "balance":
                        foreach (var row in rows)
                            result[j++][i] = row.Balance;
                        break;
                    case "firstLevel":
                        foreach (var row in rows)
                            result[j++][i] = row.FirstLevel;
                        break;
                    case "firstTime":
                        foreach (var row in rows)
                            result[j++][i] = Times[row.FirstLevel];
                        break;
                    case "lastLevel":
                        foreach (var row in rows)
                            result[j++][i] = row.LastLevel;
                        break;
                    case "lastTime":
                        foreach (var row in rows)
                            result[j++][i] = Times[row.LastLevel];
                        break;
                    case "transfersCount":
                        foreach (var row in rows)
                            result[j++][i] = row.TransfersCount;
                        break;
                    case "ticket":
                        foreach (var row in rows)
                            result[j++][i] = new TicketInfo
                            {
                                Id = row.tId,
                                Ticketer = Accounts.GetAlias(row.tTicketerId),
                                TotalSupply = row.tTotalSupply,
                                ContentType = (RawJson)Micheline.ToJson(row.RawType),
                                Content = format switch
                                {
                                    MichelineFormat.Json => row.JsonContent == null ? null : (RawJson)row.JsonContent,
                                    MichelineFormat.JsonString => row.JsonContent,
                                    MichelineFormat.Raw => row.RawContent == null ? null : (RawJson)Micheline.ToJson(row.RawContent),
                                    MichelineFormat.RawString => row.RawContent == null ? null : Micheline.ToJson(row.RawContent),
                                    _ => throw new Exception("Invalid MichelineFormat value")
                                },
                                ContentHash = row.ContentHash,
                                ContentTypeHash = row.ContentTypeHash
                            };
                        break;
                    case "ticket.id":
                        foreach (var row in rows)
                            result[j++][i] = row.tId;
                        break;
                    case "ticket.ticketer":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.tTicketerId);
                        break;
                    case "ticket.ticketer.alias":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.tTicketerId).Alias;
                        break;
                    case "ticket.ticketer.address":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.tTicketerId).Address;
                        break;
                    case "ticket.totalSupply":
                        foreach (var row in rows)
                            result[j++][i] = row.tTotalSupply;
                        break;
                    case "ticket.content":
                        foreach (var row in rows)
                            result[j++][i] = format switch
                            {
                                MichelineFormat.Json => row.JsonContent == null ? null : (RawJson)row.JsonContent,
                                MichelineFormat.JsonString => row.JsonContent,
                                MichelineFormat.Raw => row.RawContent == null ? null : (RawJson)Micheline.ToJson(row.RawContent),
                                MichelineFormat.RawString => row.RawContent == null ? null : Micheline.ToJson(row.RawContent),
                                _ => throw new Exception("Invalid MichelineFormat value")
                            };
                        break;
                    case "ticket.contentType":
                        foreach (var row in rows)
                            result[j++][i] = (RawJson)Micheline.ToJson(row.RawType);
                        break;
                    case "ticket.contentHash":
                        foreach (var row in rows)
                            result[j++][i] = row.ContentHash;
                        break;
                    case "ticket.contentTypeHash":
                        foreach (var row in rows)
                            result[j++][i] = row.ContentTypeHash;
                        break;
                }
            }

            return result;
        }
        #endregion

        #region ticket transfers
        async Task<IEnumerable<dynamic>> QueryTicketTransfersAsync(TicketTransferFilter filter, Pagination pagination, MichelineFormat format, List<SelectionField> fields = null)
        {
            var select = @"
                tr.""Id"",
                tr.""Level"",
                tr.""FromId"",
                tr.""ToId"",
                tr.""Amount"",
                tr.""TransactionId"",
                tr.""TransferTicketId"",
                tr.""SmartRollupExecuteId"",
                tr.""TicketId"" as ""tId"",
                tr.""TicketerId"" as ""tTicketerId"",
                t.""JsonContent"" as ""JsonContent"",
                t.""RawContent"" as ""RawContent"",
                t.""ContentHash"" as ""ContentHash"",
                t.""ContentTypeHash"" as ""ContentTypeHash"",
                t.""RawType"" as ""RawType"",
                t.""TotalSupply"" as ""tTotalSupply""";
            if (fields != null)
            {
                var counter = 0;
                var columns = new HashSet<string>(fields.Count);
                foreach (var field in fields)
                {
                    switch (field.Field)
                    {
                        case "id": columns.Add(@"tr.""Id"""); break;
                        case "level": columns.Add(@"tr.""Level"""); break;
                        case "timestamp": columns.Add(@"tr.""Level"""); break;
                        case "from": columns.Add(@"tr.""FromId"""); break;
                        case "to": columns.Add(@"tr.""ToId"""); break;
                        case "amount": columns.Add(@"tr.""Amount"""); break;
                        case "transactionId": columns.Add(@"tr.""TransactionId"""); break;
                        case "transferTicketId": columns.Add(@"tr.""TransferTicketId"""); break;
                        case "smartRollupExecuteId": columns.Add(@"tr.""SmartRollupExecuteId"""); break;
                        case "ticket":
                            columns.Add(@"tr.""TicketId"" as ""tId""");
                            columns.Add(@"tr.""TicketerId"" as ""tTicketerId""");
                            columns.Add(@"t.""TotalSupply"" as ""tTotalSupply""");
                            columns.Add(format switch
                            {
                                MichelineFormat.Json => @"t.""JsonContent"" as ""JsonContent""",
                                MichelineFormat.JsonString => @"t.""JsonContent"" as ""JsonContent""",
                                MichelineFormat.Raw => @"t.""RawContent"" as ""RawContent""",
                                MichelineFormat.RawString => @"t.""RawContent"" as ""RawContent""",
                                _ => throw new Exception("Invalid MichelineFormat value")
                            });
                            columns.Add(@"t.""ContentHash"" as ""ContentHash""");
                            columns.Add(@"t.""ContentTypeHash"" as ""ContentTypeHash""");
                            //TODO Should we do format switch here?
                            columns.Add(@"t.""RawType"" as ""RawType""");
                            break;
                    }
                }

                if (columns.Count == 0)
                    return Enumerable.Empty<dynamic>();

                select = string.Join(',', columns);
            }

            var sql = new SqlBuilder($@"
                SELECT {select} FROM ""TicketTransfers"" as tr
                INNER JOIN ""Tickets"" AS t ON t.""Id"" = tr.""TicketId""")
                .FilterA(@"tr.""Id""", filter.id)
                .FilterA(@"tr.""Level""", filter.level)
                .FilterA(@"tr.""Level""", filter.timestamp)
                .FilterA(filter.anyof, x => x == "from" ? @"tr.""FromId""" : @"tr.""ToId""")
                .FilterA(@"tr.""FromId""", filter.from)
                .FilterA(@"tr.""ToId""", filter.to)
                .FilterA(@"tr.""Amount""", filter.amount)
                .FilterA(@"tr.""TransactionId""", filter.transactionId)
                .FilterA(@"tr.""TransferTicketId""", filter.transferTicketId)
                .FilterA(@"tr.""SmartRollupExecuteId""", filter.smartRollupExecuteId)
                .FilterA(@"tr.""TicketId""", filter.ticket.id)
                .FilterA(@"tr.""TicketerId""", filter.ticket.ticketer)
                .FilterA(@"t.""ContentHash""", filter.ticket.contentHash)
                .FilterA(@"t.""ContentTypeHash""", filter.ticket.contentTypeHash)
                .Take(pagination, x => x switch
                {
                    "level" => (@"tr.""Level""", @"tr.""Level"""),
                    "amount" => (@"tr.""Amount""::numeric", @"tr.""Amount""::numeric"),
                    _ => (@"tr.""Id""", @"tr.""Id""")
                }, @"tr.""Id""");

            using var db = GetConnection();
            return await db.QueryAsync(sql.Query, sql.Params);
        }

        public async Task<int> GetTicketTransfersCount(TicketTransferFilter filter)
        {
            var sql = new SqlBuilder(@"
                SELECT COUNT(*) FROM ""TicketTransfers"" as tr
                INNER JOIN ""Tickets"" AS t ON t.""Id"" = tr.""TicketId""")
                .FilterA(@"tr.""Id""", filter.id)
                .FilterA(@"tr.""Level""", filter.level)
                .FilterA(@"tr.""Level""", filter.timestamp)
                .FilterA(filter.anyof, x => x == "from" ? @"tr.""FromId""" : @"tr.""ToId""")
                .FilterA(@"tr.""FromId""", filter.from)
                .FilterA(@"tr.""ToId""", filter.to)
                .FilterA(@"tr.""Amount""", filter.amount)
                .FilterA(@"tr.""TransactionId""", filter.transactionId)
                .FilterA(@"tr.""TransferTicketId""", filter.transferTicketId)
                .FilterA(@"tr.""SmartRollupExecuteId""", filter.smartRollupExecuteId)
                .FilterA(@"tr.""TicketId""", filter.ticket.id)
                .FilterA(@"tr.""TicketerId""", filter.ticket.ticketer)
                .FilterA(@"t.""ContentHash""", filter.ticket.contentHash)
                .FilterA(@"t.""ContentTypeHash""", filter.ticket.contentTypeHash);

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<TicketTransfer>> GetTicketTransfers(TicketTransferFilter filter, Pagination pagination, MichelineFormat format)
        {
            var rows = await QueryTicketTransfersAsync(filter, pagination, format);
            return rows.Select(row => new TicketTransfer
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = Times[row.Level],
                From = row.FromId == null ? null : Accounts.GetAlias(row.FromId),
                To = row.ToId == null ? null : Accounts.GetAlias(row.ToId),
                Amount = row.Amount,
                TransactionId = row.TransactionId,
                TransferTicketId = row.TransferTicketId,
                SmartRollupExecuteId = row.SmartRollupExecuteId,
                Ticket = new TicketInfo
                {
                    Id = row.tId,
                    Ticketer = Accounts.GetAlias(row.tTicketerId),
                    TotalSupply = row.tTotalSupply,
                    ContentType = (RawJson)Micheline.ToJson(row.RawType),
                    Content = format switch
                    {
                        MichelineFormat.Json => row.JsonContent == null ? null : (RawJson)row.JsonContent,
                        MichelineFormat.JsonString => row.JsonContent,
                        MichelineFormat.Raw => row.RawContent == null ? null : (RawJson)Micheline.ToJson(row.RawContent),
                        MichelineFormat.RawString => row.RawContent == null ? null : Micheline.ToJson(row.RawContent),
                        _ => throw new Exception("Invalid MichelineFormat value")
                    },
                    ContentHash = row.ContentHash,
                    ContentTypeHash = row.ContentTypeHash
                }
            });
        }

        public async Task<object[][]> GetTicketTransfers(TicketTransferFilter filter, Pagination pagination, MichelineFormat format, List<SelectionField> fields)
        {
            var rows = await QueryTicketTransfersAsync(filter, pagination, format, fields);

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
                    case "from":
                        foreach (var row in rows)
                            result[j++][i] = row.FromId == null ? null : Accounts.GetAlias(row.FromId);
                        break;
                    case "from.alias":
                        foreach (var row in rows)
                            result[j++][i] = row.FromId == null ? null : Accounts.GetAlias(row.FromId).Name;
                        break;
                    case "from.address":
                        foreach (var row in rows)
                            result[j++][i] = row.FromId == null ? null : Accounts.GetAlias(row.FromId).Address;
                        break;
                    case "to":
                        foreach (var row in rows)
                            result[j++][i] = row.ToId == null ? null : Accounts.GetAlias(row.ToId);
                        break;
                    case "to.alias":
                        foreach (var row in rows)
                            result[j++][i] = row.ToId == null ? null : Accounts.GetAlias(row.ToId).Name;
                        break;
                    case "to.address":
                        foreach (var row in rows)
                            result[j++][i] = row.ToId == null ? null : Accounts.GetAlias(row.ToId).Address;
                        break;
                    case "amount":
                        foreach (var row in rows)
                            result[j++][i] = row.Amount;
                        break;
                    case "transactionId":
                        foreach (var row in rows)
                            result[j++][i] = row.TransactionId;
                        break;
                    case "transferTicketId":
                        foreach (var row in rows)
                            result[j++][i] = row.OriginationId;
                        break;
                    case "smartRollupExecuteId":
                        foreach (var row in rows)
                            result[j++][i] = row.MigrationId;
                        break;
                    case "ticket":
                        foreach (var row in rows)
                            result[j++][i] = new TicketInfo
                            {
                                Id = row.tId,
                                Ticketer = Accounts.GetAlias(row.tTicketerId),
                                TotalSupply = row.tTotalSupply,
                                ContentType = (RawJson)Micheline.ToJson(row.RawType),
                                Content = format switch
                                {
                                    MichelineFormat.Json => row.JsonContent == null ? null : (RawJson)row.JsonContent,
                                    MichelineFormat.JsonString => row.JsonContent,
                                    MichelineFormat.Raw => row.RawContent == null ? null : (RawJson)Micheline.ToJson(row.RawContent),
                                    MichelineFormat.RawString => row.RawContent == null ? null : Micheline.ToJson(row.RawContent),
                                    _ => throw new Exception("Invalid MichelineFormat value")
                                },
                                ContentHash = row.ContentHash,
                                ContentTypeHash = row.ContentTypeHash
                            };
                        break;
                    case "ticket.id":
                        foreach (var row in rows)
                            result[j++][i] = row.tId;
                        break;
                    case "ticket.ticketer":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.tTicketerId);
                        break;
                    case "ticket.ticketer.alias":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.tTicketerId).Alias;
                        break;
                    case "ticket.ticketer.address":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.tTicketerId).Address;
                        break;
                    case "ticket.totalSupply":
                        foreach (var row in rows)
                            result[j++][i] = row.tTotalSupply;
                        break;
                    case "ticket.content":
                        foreach (var row in rows)
                            result[j++][i] = format switch
                            {
                                MichelineFormat.Json => row.JsonContent == null ? null : (RawJson)row.JsonContent,
                                MichelineFormat.JsonString => row.JsonContent,
                                MichelineFormat.Raw => row.RawContent == null ? null : (RawJson)Micheline.ToJson(row.RawContent),
                                MichelineFormat.RawString => row.RawContent == null ? null : Micheline.ToJson(row.RawContent),
                                _ => throw new Exception("Invalid MichelineFormat value")
                            };
                        break;
                    case "ticket.contentType":
                        foreach (var row in rows)
                            result[j++][i] = (RawJson)Micheline.ToJson(row.RawType);
                        break;
                    case "ticket.contentHash":
                        foreach (var row in rows)
                            result[j++][i] = row.ContentHash;
                        break;
                    case "ticket.contentTypeHash":
                        foreach (var row in rows)
                            result[j++][i] = row.ContentTypeHash;
                        break;
                }
            }

            return result;
        }
        #endregion

        #region historical balances
        async Task<IEnumerable<dynamic>> QueryHistoricalTicketBalancesAsync(int level, TicketBalanceShortFilter filter, Pagination pagination, MichelineFormat format, List<SelectionField> fields = null)
        {
            var select = @"
                tb.""AccountId"",
                tb.""Balance"",
                tb.""TicketId"" as ""tId"",
                t.""TicketerId"" as ""tTicketerId""";
            if (fields != null)
            {
                var counter = 0;
                var columns = new HashSet<string>(fields.Count);
                foreach (var field in fields)
                {
                    switch (field.Field)
                    {
                        case "account": columns.Add(@"tb.""AccountId"""); break;
                        case "balance": columns.Add(@"tb.""Balance"""); break;
                        case "ticket":
                            columns.Add(@"tb.""TicketId"" as ""tId""");
                            columns.Add(@"t.""TicketerId"" as ""tTicketerId""");
                            break;
                    }
                }

                if (columns.Count == 0)
                    return Enumerable.Empty<dynamic>();

                select = string.Join(',', columns);
            }

            var sql = new SqlBuilder()
                .Append($@"SELECT {select} FROM (")
                    .Append(@"SELECT ROW_NUMBER() over (ORDER BY ""TicketId"", ""AccountId"") as ""Id"", ""TicketId"", ""AccountId"", SUM(""Amount"")::text AS ""Balance"" FROM (")
                        
                        .Append(@"SELECT tr.""TicketId"", tr.""FromId"" AS ""AccountId"", -tr.""Amount""::numeric AS ""Amount"" FROM ""TicketTransfers"" as tr")
                        .Append(@"INNER JOIN ""Tickets"" AS t ON t.""Id"" = tr.""TicketId""")
                        .Filter($@"tr.""Level"" <= {level}")
                        .Filter($@"tr.""FromId"" IS NOT NULL")
                        .FilterA(@"tr.""FromId""", filter.account)
                        .FilterA(@"tr.""TicketId""", filter.ticket.id)
                        .FilterA(@"t.""TicketerId""", filter.ticket.ticketer)
                        .FilterA(@"t.""ContentHash""", filter.ticket.contentHash)
                        .FilterA(@"t.""ContentTypeHash""", filter.ticket.contentTypeHash)
                .ResetFilters()

                        .Append("UNION ALL")

                        .Append(@"SELECT tr.""TicketId"", tr.""ToId"" AS ""AccountId"", tr.""Amount""::numeric AS ""Amount"" FROM ""TicketTransfers"" as tr")
                        .Append(@"INNER JOIN ""Tickets"" AS t ON t.""Id"" = tr.""TicketId""")
                        .Filter($@"tr.""Level"" <= {level}")
                        .Filter($@"tr.""ToId"" IS NOT NULL")
                        .FilterA(@"tr.""ToId""", filter.account)
                        .FilterA(@"tr.""TicketId""", filter.ticket.id)
                        .FilterA(@"t.""TicketerId""", filter.ticket.ticketer)
                        .FilterA(@"t.""ContentHash""", filter.ticket.contentHash)
                        .FilterA(@"t.""ContentTypeHash""", filter.ticket.contentTypeHash)
                        .ResetFilters()

                    .Append(") as tb")
                    .Append(@"GROUP BY tb.""TicketId"", tb.""AccountId""")
                .Append(") as tb")
                .Append(@"INNER JOIN ""Tickets"" AS t ON t.""Id"" = tb.""TicketId""")
                .FilterA(@"tb.""Balance""", filter.balance)
                .Take(pagination, _ => (@"""Balance""::numeric", @"""Balance""::numeric"), @"tb.""Id""");

            using var db = GetConnection();
            return await db.QueryAsync(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<TicketBalanceShort>> GetHistoricalTicketBalances(int level, TicketBalanceShortFilter filter, Pagination pagination, MichelineFormat format)
        {
            var rows = await QueryHistoricalTicketBalancesAsync(level, filter, pagination, format);
            return rows.Select(row => new TicketBalanceShort
            {
                Account = Accounts.GetAlias(row.AccountId),
                Balance = row.Balance,
                Ticket = new TicketInfo
                {
                    Id = row.tId,
                    Ticketer = Accounts.GetAlias(row.tTicketerId)
                }
            });
        }

        public async Task<object[][]> GetHistoricalTicketBalances(int level, TicketBalanceShortFilter filter, Pagination pagination, MichelineFormat format, List<SelectionField> fields)
        {
            var rows = await QueryHistoricalTicketBalancesAsync(level, filter, pagination, format, fields);

            var result = new object[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object[fields.Count];

            for (int i = 0, j = 0; i < fields.Count; j = 0, i++)
            {
                switch (fields[i].Full)
                {
                    case "account":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.AccountId);
                        break;
                    case "account.alias":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.AccountId).Name;
                        break;
                    case "account.address":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.AccountId).Address;
                        break;
                    case "balance":
                        foreach (var row in rows)
                            result[j++][i] = row.Balance;
                        break;
                    case "ticket":
                        foreach (var row in rows)
                            result[j++][i] = new TicketInfo
                            {
                                Id = row.tId,
                                Ticketer = Accounts.GetAlias(row.tTicketerId)
                            };
                        break;
                    case "ticket.id":
                        foreach (var row in rows)
                            result[j++][i] = row.tId;
                        break;
                    case "ticket.ticketer":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.tTicketerId);
                        break;
                    case "ticket.ticketer.alias":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.tTicketerId).Alias;
                        break;
                    case "ticket.ticketer.address":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.tTicketerId).Address;
                        break;
                }
            }

            return result;
        }
        #endregion
    }
}
