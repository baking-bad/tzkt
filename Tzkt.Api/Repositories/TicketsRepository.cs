using Dapper;
using Netmavryk.Encoding;
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
        async Task<IEnumerable<dynamic>> QueryTicketsAsync(TicketFilter filter, Pagination pagination, List<SelectionField> fields = null)
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
                        case "rawType": columns.Add(@"""RawType"""); break;
                        case "rawContent": columns.Add(@"""RawContent"""); break;
                        case "content":
                            if (field.Path == null)
                            {
                                columns.Add(@"""JsonContent""");
                            }
                            else
                            {
                                field.Column = $"c{counter++}";
                                columns.Add($@"""JsonContent"" #> '{{{field.PathString}}}' as {field.Column}");
                            }
                            break;
                        case "typeHash": columns.Add(@"""TypeHash"""); break;
                        case "contentHash": columns.Add(@"""ContentHash"""); break;
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
                    }
                }

                if (columns.Count == 0)
                    return Enumerable.Empty<dynamic>();

                select = string.Join(',', columns);
            }

            var sql = new SqlBuilder($@"SELECT {select} FROM ""Tickets""")
                .Filter("Id", filter.id)
                .Filter("TicketerId", filter.ticketer)
                .Filter("RawType", filter.rawType)
                .Filter("RawContent", filter.rawContent)
                .Filter("JsonContent", filter.content)
                .Filter("TypeHash", filter.typeHash)
                .Filter("ContentHash", filter.contentHash)
                .Filter("FirstMinterId", filter.firstMinter)
                .Filter("FirstLevel", filter.firstLevel)
                .Filter("FirstLevel", filter.firstTime)
                .Filter("LastLevel", filter.lastLevel)
                .Filter("LastLevel", filter.lastTime)
                .Take(pagination, x => x switch
                {
                    "firstLevel" => (@"""FirstLevel""", @"""FirstLevel"""),
                    "lastLevel" => (@"""LastLevel""", @"""LastLevel"""),
                    "transfersCount" => (@"""TransfersCount""", @"""TransfersCount"""),
                    "balancesCount" => (@"""BalancesCount""", @"""BalancesCount"""),
                    "holdersCount" => (@"""HoldersCount""", @"""HoldersCount"""),
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
                .Filter("RawType", filter.rawType)
                .Filter("RawContent", filter.rawContent)
                .Filter("JsonContent", filter.content)
                .Filter("TypeHash", filter.typeHash)
                .Filter("ContentHash", filter.contentHash)
                .Filter("FirstMinterId", filter.firstMinter)
                .Filter("FirstLevel", filter.firstLevel)
                .Filter("FirstLevel", filter.firstTime)
                .Filter("LastLevel", filter.lastLevel)
                .Filter("LastLevel", filter.lastTime);

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<Ticket>> GetTickets(TicketFilter filter, Pagination pagination)
        {
            var rows = await QueryTicketsAsync(filter, pagination);
            return rows.Select(row => new Ticket
            {
                Id = row.Id,
                Ticketer = Accounts.GetAlias(row.TicketerId),
                RawType = Micheline.FromBytes((byte[])row.RawType),
                RawContent = Micheline.FromBytes((byte[])row.RawContent),
                Content = (RawJson)row.JsonContent,
                TypeHash = row.TypeHash,
                ContentHash = row.ContentHash,
                FirstMinter = Accounts.GetAlias(row.FirstMinterId),
                FirstLevel = row.FirstLevel,
                FirstTime = Times[row.FirstLevel],
                LastLevel = row.LastLevel,
                LastTime = Times[row.LastLevel],
                TransfersCount = row.TransfersCount,
                BalancesCount = row.BalancesCount,
                HoldersCount = row.HoldersCount,
                TotalMinted = row.TotalMinted,
                TotalBurned = row.TotalBurned,
                TotalSupply = row.TotalSupply
            });
        }

        public async Task<object[][]> GetTickets(TicketFilter filter, Pagination pagination, List<SelectionField> fields)
        {
            var rows = await QueryTicketsAsync(filter, pagination, fields);

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
                    case "rawType":
                        foreach (var row in rows)
                            result[j++][i] = Micheline.FromBytes((byte[])row.RawType);
                        break;
                    case "rawContent":
                        foreach (var row in rows)
                            result[j++][i] = Micheline.FromBytes((byte[])row.RawContent);
                        break;
                    case "content":
                        foreach (var row in rows)
                            result[j++][i] = (RawJson)row.JsonContent;
                        break;
                    case "typeHash":
                        foreach (var row in rows)
                            result[j++][i] = row.TypeHash;
                        break;
                    case "contentHash":
                        foreach (var row in rows)
                            result[j++][i] = row.ContentHash;
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
        async Task<IEnumerable<dynamic>> QueryTicketBalancesAsync(TicketBalanceFilter filter, Pagination pagination, List<SelectionField> fields = null)
        {
            var select = @"
                tb.""Id"",
                tb.""AccountId"",
                tb.""Balance"",
                tb.""TransfersCount"",
                tb.""FirstLevel"",
                tb.""LastLevel"",
                tb.""TicketId"" as ""tId"",
                tb.""TicketerId"" as ""tTicketerId"",
                t.""RawType"" as ""tRawType"",
                t.""RawContent"" as ""tRawContent"",
                t.""JsonContent"" as ""tJsonContent"",
                t.""TypeHash"" as ""tTypeHash"",
                t.""ContentHash"" as ""tContentHash"",
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
                        case "ticket":
                            if (field.Path == null)
                            {
                                columns.Add(@"tb.""TicketId"" as ""tId""");
                                columns.Add(@"tb.""TicketerId"" as ""tTicketerId""");
                                columns.Add(@"t.""RawType"" as ""tRawType""");
                                columns.Add(@"t.""RawContent"" as ""tRawContent""");
                                columns.Add(@"t.""JsonContent"" as ""tJsonContent""");
                                columns.Add(@"t.""TypeHash"" as ""tTypeHash""");
                                columns.Add(@"t.""ContentHash"" as ""tContentHash""");
                                columns.Add(@"t.""TotalSupply"" as ""tTotalSupply""");
                            }
                            else
                            {
                                var subField = field.SubField();
                                switch (subField.Field)
                                {
                                    case "id": columns.Add(@"tb.""TicketId"" as ""tId"""); break;
                                    case "ticketer": columns.Add(@"tb.""TicketerId"" as ""tTicketerId"""); break;
                                    case "rawType": columns.Add(@"t.""RawType"" as ""tRawType"""); break;
                                    case "rawContent": columns.Add(@"t.""RawContent"" as ""tRawContent"""); break;
                                    case "content":
                                        if (subField.Path == null)
                                        {
                                            columns.Add(@"t.""JsonContent"" as ""tJsonContent""");
                                        }
                                        else
                                        {
                                            field.Column = $"c{counter++}";
                                            columns.Add($@"t.""JsonContent"" #> '{{{subField.PathString}}}' as {field.Column}");
                                        }
                                        break;
                                    case "typeHash": columns.Add(@"t.""TypeHash"" as ""tTypeHash"""); break;
                                    case "contentHash": columns.Add(@"t.""ContentHash"" as ""tContentHash"""); break;
                                    case "totalSupply": columns.Add(@"t.""TotalSupply"" as ""tTotalSupply"""); break;
                                }
                            }
                            break;
                        case "account": columns.Add(@"tb.""AccountId"""); break;
                        case "balance": columns.Add(@"tb.""Balance"""); break;
                        case "transfersCount": columns.Add(@"tb.""TransfersCount"""); break;
                        case "firstLevel": columns.Add(@"tb.""FirstLevel"""); break;
                        case "firstTime": columns.Add(@"tb.""FirstLevel"""); break;
                        case "lastLevel": columns.Add(@"tb.""LastLevel"""); break;
                        case "lastTime": columns.Add(@"tb.""LastLevel"""); break;
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
                .FilterA(@"tb.""TransfersCount""", filter.transfersCount)
                .FilterA(@"tb.""FirstLevel""", filter.firstLevel)
                .FilterA(@"tb.""FirstLevel""", filter.firstTime)
                .FilterA(@"tb.""LastLevel""", filter.lastLevel)
                .FilterA(@"tb.""LastLevel""", filter.lastTime)
                .FilterA(@"tb.""TicketId""", filter.ticket.id)
                .FilterA(@"tb.""TicketerId""", filter.ticket.ticketer)
                .FilterA(@"t.""RawType""", filter.ticket.rawType)
                .FilterA(@"t.""RawContent""", filter.ticket.rawContent)
                .FilterA(@"t.""JsonContent""", filter.ticket.content)
                .FilterA(@"t.""TypeHash""", filter.ticket.typeHash)
                .FilterA(@"t.""ContentHash""", filter.ticket.contentHash)
                .Take(pagination, x => x switch
                {
                    "balance" => (@"tb.""Balance""::numeric", @"tb.""Balance""::numeric"),
                    "transfersCount" => (@"tb.""TransfersCount""", @"tb.""TransfersCount"""),
                    "firstLevel" => (@"tb.""FirstLevel""", @"tb.""FirstLevel"""),
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
                .FilterA(@"tb.""TransfersCount""", filter.transfersCount)
                .FilterA(@"tb.""FirstLevel""", filter.firstLevel)
                .FilterA(@"tb.""FirstLevel""", filter.firstTime)
                .FilterA(@"tb.""LastLevel""", filter.lastLevel)
                .FilterA(@"tb.""LastLevel""", filter.lastTime)
                .FilterA(@"tb.""TicketId""", filter.ticket.id)
                .FilterA(@"tb.""TicketerId""", filter.ticket.ticketer)
                .FilterA(@"t.""RawType""", filter.ticket.rawType)
                .FilterA(@"t.""RawContent""", filter.ticket.rawContent)
                .FilterA(@"t.""JsonContent""", filter.ticket.content)
                .FilterA(@"t.""TypeHash""", filter.ticket.typeHash)
                .FilterA(@"t.""ContentHash""", filter.ticket.contentHash);

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<TicketBalance>> GetTicketBalances(TicketBalanceFilter filter, Pagination pagination)
        {
            var rows = await QueryTicketBalancesAsync(filter, pagination);
            return rows.Select(row => new TicketBalance
            {
                Id = row.Id,
                Ticket = new TicketInfo
                {
                    Id = row.tId,
                    Ticketer = Accounts.GetAlias(row.tTicketerId),
                    RawType = Micheline.FromBytes((byte[])row.tRawType),
                    RawContent = Micheline.FromBytes((byte[])row.tRawContent),
                    Content = (RawJson)row.tJsonContent,
                    TypeHash = row.tTypeHash,
                    ContentHash = row.tContentHash,
                    TotalSupply = row.tTotalSupply
                },
                Account = Accounts.GetAlias(row.AccountId),
                Balance = row.Balance,
                TransfersCount = row.TransfersCount,
                FirstLevel = row.FirstLevel,
                FirstTime = Times[row.FirstLevel],
                LastLevel = row.LastLevel,
                LastTime = Times[row.LastLevel]
            });
        }

        public async Task<object[][]> GetTicketBalances(TicketBalanceFilter filter, Pagination pagination, List<SelectionField> fields)
        {
            var rows = await QueryTicketBalancesAsync(filter, pagination, fields);

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
                    case "transfersCount":
                        foreach (var row in rows)
                            result[j++][i] = row.TransfersCount;
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
                    case "ticket":
                        foreach (var row in rows)
                            result[j++][i] = new TicketInfo
                            {
                                Id = row.tId,
                                Ticketer = Accounts.GetAlias(row.tTicketerId),
                                RawType = Micheline.FromBytes((byte[])row.tRawType),
                                RawContent = Micheline.FromBytes((byte[])row.tRawContent),
                                Content = (RawJson)row.tJsonContent,
                                TypeHash = row.tTypeHash,
                                ContentHash = row.tContentHash,
                                TotalSupply = row.tTotalSupply
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
                    case "ticket.rawType":
                        foreach (var row in rows)
                            result[j++][i] = Micheline.FromBytes((byte[])row.tRawType);
                        break;
                    case "ticket.rawContent":
                        foreach (var row in rows)
                            result[j++][i] = Micheline.FromBytes((byte[])row.tRawContent);
                        break;
                    case "ticket.content":
                        foreach (var row in rows)
                            result[j++][i] = (RawJson)row.tJsonContent;
                        break;
                    case "ticket.typeHash":
                        foreach (var row in rows)
                            result[j++][i] = row.tTypeHash;
                        break;
                    case "ticket.contentHash":
                        foreach (var row in rows)
                            result[j++][i] = row.tContentHash;
                        break;
                    case "ticket.totalSupply":
                        foreach (var row in rows)
                            result[j++][i] = row.tTotalSupply;
                        break;
                    default:
                        if (fields[i].Full.StartsWith("ticket.content."))
                            foreach (var row in rows)
                                result[j++][i] = (RawJson)((row as IDictionary<string, object>)[fields[i].Column] as string);
                        break;
                }
            }

            return result;
        }
        #endregion

        #region ticket transfers
        async Task<IEnumerable<dynamic>> QueryTicketTransfersAsync(TicketTransferFilter filter, Pagination pagination, List<SelectionField> fields = null)
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
                t.""RawType"" as ""tRawType"",
                t.""RawContent"" as ""tRawContent"",
                t.""JsonContent"" as ""tJsonContent"",
                t.""TypeHash"" as ""tTypeHash"",
                t.""ContentHash"" as ""tContentHash"",
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
                        case "ticket":
                            if (field.Path == null)
                            {
                                columns.Add(@"tr.""TicketId"" as ""tId""");
                                columns.Add(@"tr.""TicketerId"" as ""tTicketerId""");
                                columns.Add(@"t.""RawType"" as ""tRawType""");
                                columns.Add(@"t.""RawContent"" as ""tRawContent""");
                                columns.Add(@"t.""JsonContent"" as ""tJsonContent""");
                                columns.Add(@"t.""TypeHash"" as ""tTypeHash""");
                                columns.Add(@"t.""ContentHash"" as ""tContentHash""");
                                columns.Add(@"t.""TotalSupply"" as ""tTotalSupply""");
                            }
                            else
                            {
                                var subField = field.SubField();
                                switch (subField.Field)
                                {
                                    case "id": columns.Add(@"tr.""TicketId"" as ""tId"""); break;
                                    case "ticketer": columns.Add(@"tr.""TicketerId"" as ""tTicketerId"""); break;
                                    case "rawType": columns.Add(@"t.""RawType"" as ""tRawType"""); break;
                                    case "rawContent": columns.Add(@"t.""RawContent"" as ""tRawContent"""); break;
                                    case "content":
                                        if (subField.Path == null)
                                        {
                                            columns.Add(@"t.""JsonContent"" as ""tJsonContent""");
                                        }
                                        else
                                        {
                                            field.Column = $"c{counter++}";
                                            columns.Add($@"t.""JsonContent"" #> '{{{subField.PathString}}}' as {field.Column}");
                                        }
                                        break;
                                    case "typeHash": columns.Add(@"t.""TypeHash"" as ""tTypeHash"""); break;
                                    case "contentHash": columns.Add(@"t.""ContentHash"" as ""tContentHash"""); break;
                                    case "totalSupply": columns.Add(@"t.""TotalSupply"" as ""tTotalSupply"""); break;
                                }
                            }
                            break;
                        case "from": columns.Add(@"tr.""FromId"""); break;
                        case "to": columns.Add(@"tr.""ToId"""); break;
                        case "amount": columns.Add(@"tr.""Amount"""); break;
                        case "transactionId": columns.Add(@"tr.""TransactionId"""); break;
                        case "transferTicketId": columns.Add(@"tr.""TransferTicketId"""); break;
                        case "smartRollupExecuteId": columns.Add(@"tr.""SmartRollupExecuteId"""); break;
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
                .FilterA(@"t.""RawType""", filter.ticket.rawType)
                .FilterA(@"t.""RawContent""", filter.ticket.rawContent)
                .FilterA(@"t.""JsonContent""", filter.ticket.content)
                .FilterA(@"t.""TypeHash""", filter.ticket.typeHash)
                .FilterA(@"t.""ContentHash""", filter.ticket.contentHash)
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
                .FilterA(@"t.""RawType""", filter.ticket.rawType)
                .FilterA(@"t.""RawContent""", filter.ticket.rawContent)
                .FilterA(@"t.""JsonContent""", filter.ticket.content)
                .FilterA(@"t.""TypeHash""", filter.ticket.typeHash)
                .FilterA(@"t.""ContentHash""", filter.ticket.contentHash);

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<TicketTransfer>> GetTicketTransfers(TicketTransferFilter filter, Pagination pagination)
        {
            var rows = await QueryTicketTransfersAsync(filter, pagination);
            return rows.Select(row => new TicketTransfer
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = Times[row.Level],
                Ticket = new TicketInfo
                {
                    Id = row.tId,
                    Ticketer = Accounts.GetAlias(row.tTicketerId),
                    RawType = Micheline.FromBytes((byte[])row.tRawType),
                    RawContent = Micheline.FromBytes((byte[])row.tRawContent),
                    Content = (RawJson)row.tJsonContent,
                    TypeHash = row.tTypeHash,
                    ContentHash = row.tContentHash,
                    TotalSupply = row.tTotalSupply
                },
                From = row.FromId == null ? null : Accounts.GetAlias(row.FromId),
                To = row.ToId == null ? null : Accounts.GetAlias(row.ToId),
                Amount = row.Amount,
                TransactionId = row.TransactionId,
                TransferTicketId = row.TransferTicketId,
                SmartRollupExecuteId = row.SmartRollupExecuteId
            });
        }

        public async Task<object[][]> GetTicketTransfers(TicketTransferFilter filter, Pagination pagination, List<SelectionField> fields)
        {
            var rows = await QueryTicketTransfersAsync(filter, pagination, fields);

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
                                RawType = Micheline.FromBytes((byte[])row.tRawType),
                                RawContent = Micheline.FromBytes((byte[])row.tRawContent),
                                Content = (RawJson)row.tJsonContent,
                                TypeHash = row.tTypeHash,
                                ContentHash = row.tContentHash,
                                TotalSupply = row.tTotalSupply
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
                    case "ticket.rawType":
                        foreach (var row in rows)
                            result[j++][i] = Micheline.FromBytes((byte[])row.tRawType);
                        break;
                    case "ticket.rawContent":
                        foreach (var row in rows)
                            result[j++][i] = Micheline.FromBytes((byte[])row.tRawContent);
                        break;
                    case "ticket.content":
                        foreach (var row in rows)
                            result[j++][i] = (RawJson)row.tJsonContent;
                        break;
                    case "ticket.typeHash":
                        foreach (var row in rows)
                            result[j++][i] = row.tTypeHash;
                        break;
                    case "ticket.contentHash":
                        foreach (var row in rows)
                            result[j++][i] = row.tContentHash;
                        break;
                    case "ticket.totalSupply":
                        foreach (var row in rows)
                            result[j++][i] = row.tTotalSupply;
                        break;
                    default:
                        if (fields[i].Full.StartsWith("ticket.content."))
                            foreach (var row in rows)
                                result[j++][i] = (RawJson)((row as IDictionary<string, object>)[fields[i].Column] as string);
                        break;
                }
            }

            return result;
        }
        #endregion

        #region historical balances
        async Task<IEnumerable<dynamic>> QueryHistoricalTicketBalancesAsync(int level, TicketBalanceShortFilter filter, Pagination pagination, List<SelectionField> fields = null)
        {
            var select = @"
                tb.""Id"",
                tb.""AccountId"",
                tb.""Balance"",
                tb.""TicketId"" as ""tId"",
                tb.""TicketerId"" as ""tTicketerId"",
                t.""RawType"" as ""tRawType"",
                t.""RawContent"" as ""tRawContent"",
                t.""JsonContent"" as ""tJsonContent"",
                t.""TypeHash"" as ""tTypeHash"",
                t.""ContentHash"" as ""tContentHash""";
            if (fields != null)
            {
                var counter = 0;
                var columns = new HashSet<string>(fields.Count);
                foreach (var field in fields)
                {
                    switch (field.Field)
                    {
                        case "id": columns.Add(@"tb.""Id"""); break;
                        case "ticket":
                            if (field.Path == null)
                            {
                                columns.Add(@"tb.""TicketId"" as ""tId""");
                                columns.Add(@"tb.""TicketerId"" as ""tTicketerId""");
                                columns.Add(@"t.""RawType"" as ""tRawType""");
                                columns.Add(@"t.""RawContent"" as ""tRawContent""");
                                columns.Add(@"t.""JsonContent"" as ""tJsonContent""");
                                columns.Add(@"t.""TypeHash"" as ""tTypeHash""");
                                columns.Add(@"t.""ContentHash"" as ""tContentHash""");
                            }
                            else
                            {
                                var subField = field.SubField();
                                switch (subField.Field)
                                {
                                    case "id": columns.Add(@"tb.""TicketId"" as ""tId"""); break;
                                    case "ticketer": columns.Add(@"tb.""TicketerId"" as ""tTicketerId"""); break;
                                    case "rawType": columns.Add(@"t.""RawType"" as ""tRawType"""); break;
                                    case "rawContent": columns.Add(@"t.""RawContent"" as ""tRawContent"""); break;
                                    case "content":
                                        if (subField.Path == null)
                                        {
                                            columns.Add(@"t.""JsonContent"" as ""tJsonContent""");
                                        }
                                        else
                                        {
                                            field.Column = $"c{counter++}";
                                            columns.Add($@"t.""JsonContent"" #> '{{{subField.PathString}}}' as {field.Column}");
                                        }
                                        break;
                                    case "typeHash": columns.Add(@"t.""TypeHash"" as ""tTypeHash"""); break;
                                    case "contentHash": columns.Add(@"t.""ContentHash"" as ""tContentHash"""); break;
                                }
                            }
                            break;
                        case "account": columns.Add(@"tb.""AccountId"""); break;
                        case "balance": columns.Add(@"tb.""Balance"""); break;
                    }
                }

                if (columns.Count == 0)
                    return Enumerable.Empty<dynamic>();

                select = string.Join(',', columns);
            }

            var sql = new SqlBuilder()
                .Append($@"SELECT {select} FROM (")
                    .Append(@"SELECT ROW_NUMBER() over (ORDER BY ""TicketId"", ""AccountId"") as ""Id"", ""TicketId"", ""TicketerId"", ""AccountId"", SUM(""Amount"")::text AS ""Balance"" FROM (")
                        
                        .Append(@"SELECT ""TicketId"", ""TicketerId"", ""FromId"" AS ""AccountId"", -""Amount""::numeric AS ""Amount"" FROM ""TicketTransfers""")
                        .Filter($@"""Level"" <= {level}")
                        .Filter($@"""FromId"" IS NOT NULL")
                        .FilterA(@"""FromId""", filter.account)
                        .FilterA(@"""TicketId""", filter.ticket.id)
                        .FilterA(@"""TicketerId""", filter.ticket.ticketer)
                        .ResetFilters()

                        .Append("UNION ALL")

                        .Append(@"SELECT ""TicketId"", ""TicketerId"", ""ToId"" AS ""AccountId"", ""Amount""::numeric AS ""Amount"" FROM ""TicketTransfers""")
                        .Filter($@"""Level"" <= {level}")
                        .Filter($@"""ToId"" IS NOT NULL")
                        .FilterA(@"""ToId""", filter.account)
                        .FilterA(@"""TicketId""", filter.ticket.id)
                        .FilterA(@"""TicketerId""", filter.ticket.ticketer)
                        .ResetFilters()

                    .Append(") as tb")
                    .Append(@"GROUP BY tb.""TicketId"", tb.""TicketerId"", tb.""AccountId""")
                .Append(") as tb")
                .Append(@"INNER JOIN ""Tickets"" AS t ON t.""Id"" = tb.""TicketId""")
                .FilterA(@"tb.""Id""", filter.id)
                .FilterA(@"tb.""Balance""", filter.balance)
                .Take(pagination, _ => (@"tb.""Id""", @"tb.""Id"""), @"tb.""Id""");

            using var db = GetConnection();
            return await db.QueryAsync(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<TicketBalanceShort>> GetHistoricalTicketBalances(int level, TicketBalanceShortFilter filter, Pagination pagination)
        {
            var rows = await QueryHistoricalTicketBalancesAsync(level, filter, pagination);
            return rows.Select(row => new TicketBalanceShort
            {
                Id = row.Id,
                Account = Accounts.GetAlias(row.AccountId),
                Balance = row.Balance,
                Ticket = new TicketInfoShort
                {
                    Id = row.tId,
                    Ticketer = Accounts.GetAlias(row.tTicketerId),
                    RawType = Micheline.FromBytes((byte[])row.tRawType),
                    RawContent = Micheline.FromBytes((byte[])row.tRawContent),
                    Content = (RawJson)row.tJsonContent,
                    TypeHash = row.tTypeHash,
                    ContentHash = row.tContentHash
                }
            });
        }

        public async Task<object[][]> GetHistoricalTicketBalances(int level, TicketBalanceShortFilter filter, Pagination pagination, List<SelectionField> fields)
        {
            var rows = await QueryHistoricalTicketBalancesAsync(level, filter, pagination, fields);

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
                    case "ticket":
                        foreach (var row in rows)
                            result[j++][i] = new TicketInfoShort
                            {
                                Id = row.tId,
                                Ticketer = Accounts.GetAlias(row.tTicketerId),
                                RawType = Micheline.FromBytes((byte[])row.tRawType),
                                RawContent = Micheline.FromBytes((byte[])row.tRawContent),
                                Content = (RawJson)row.tJsonContent,
                                TypeHash = row.tTypeHash,
                                ContentHash = row.tContentHash
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
                    case "ticket.rawType":
                        foreach (var row in rows)
                            result[j++][i] = Micheline.FromBytes((byte[])row.tRawType);
                        break;
                    case "ticket.rawContent":
                        foreach (var row in rows)
                            result[j++][i] = Micheline.FromBytes((byte[])row.tRawContent);
                        break;
                    case "ticket.content":
                        foreach (var row in rows)
                            result[j++][i] = (RawJson)row.tJsonContent;
                        break;
                    case "ticket.typeHash":
                        foreach (var row in rows)
                            result[j++][i] = row.tTypeHash;
                        break;
                    case "ticket.contentHash":
                        foreach (var row in rows)
                            result[j++][i] = row.tContentHash;
                        break;
                    default:
                        if (fields[i].Full.StartsWith("ticket.content."))
                            foreach (var row in rows)
                                result[j++][i] = (RawJson)((row as IDictionary<string, object>)[fields[i].Column] as string);
                        break;
                }
            }

            return result;
        }
        #endregion
    }
}
