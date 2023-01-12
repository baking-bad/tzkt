using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Tzkt.Api.Models;
using Tzkt.Data;

namespace Tzkt.Api.Repositories
{
    public partial class OperationRepository : DbConnection
    {
        public async Task<bool?> GetDelegationStatus(string hash)
        {
            using var db = GetConnection();
            return await GetStatus(db, nameof(TzktContext.DelegationOps), hash);
        }

        public async Task<int> GetDelegationsCount(
            Int32Parameter level,
            DateTimeParameter timestamp)
        {
            var sql = new SqlBuilder(@"SELECT COUNT(*) FROM ""DelegationOps""")
                .Filter("Level", level)
                .Filter("Timestamp", timestamp);

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<DelegationOperation>> GetDelegations(string hash, Symbols quote)
        {
            var sql = @"
                SELECT      o.""Id"", o.""Level"", o.""Timestamp"", o.""SenderId"", o.""InitiatorId"", o.""Counter"", o.""BakerFee"",
                            o.""GasLimit"", o.""GasUsed"", o.""StorageLimit"", o.""Status"", o.""Nonce"", o.""Amount"", o.""PrevDelegateId"", o.""DelegateId"", o.""Errors"", b.""Hash""
                FROM        ""DelegationOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                WHERE       o.""OpHash"" = @hash::character(51)
                ORDER BY    o.""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash });

            return rows.Select(row => new DelegationOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Hash = hash,
                Initiator = row.InitiatorId != null ? Accounts.GetAlias(row.InitiatorId) : null,
                Sender = Accounts.GetAlias(row.SenderId),
                SenderCodeHash = row.SenderCodeHash,
                Counter = row.Counter,
                Nonce = row.Nonce,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                StorageLimit = row.StorageLimit,
                BakerFee = row.BakerFee,
                Amount = row.Amount,
                PrevDelegate = row.PrevDelegateId != null ? Accounts.GetAlias(row.PrevDelegateId) : null,
                NewDelegate = row.DelegateId != null ? Accounts.GetAlias(row.DelegateId) : null,
                Status = OpStatuses.ToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<IEnumerable<DelegationOperation>> GetDelegations(string hash, int counter, Symbols quote)
        {
            var sql = @"
                SELECT      o.""Id"", o.""Level"", o.""Timestamp"", o.""SenderId"", o.""InitiatorId"", o.""BakerFee"",
                            o.""GasLimit"", o.""GasUsed"", o.""StorageLimit"", o.""Status"", o.""Nonce"", o.""Amount"", o.""PrevDelegateId"", o.""DelegateId"", o.""Errors"", b.""Hash""
                FROM        ""DelegationOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                WHERE       o.""OpHash"" = @hash::character(51) AND o.""Counter"" = @counter
                ORDER BY    o.""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash, counter });

            return rows.Select(row => new DelegationOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Hash = hash,
                Initiator = row.InitiatorId != null ? Accounts.GetAlias(row.InitiatorId) : null,
                Sender = Accounts.GetAlias(row.SenderId),
                SenderCodeHash = row.SenderCodeHash,
                Counter = counter,
                Nonce = row.Nonce,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                StorageLimit = row.StorageLimit,
                BakerFee = row.BakerFee,
                Amount = row.Amount,
                PrevDelegate = row.PrevDelegateId != null ? Accounts.GetAlias(row.PrevDelegateId) : null,
                NewDelegate = row.DelegateId != null ? Accounts.GetAlias(row.DelegateId) : null,
                Status = OpStatuses.ToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<IEnumerable<DelegationOperation>> GetDelegations(string hash, int counter, int nonce, Symbols quote)
        {
            var sql = @"
                SELECT      o.""Id"", o.""Level"", o.""Timestamp"", o.""SenderId"", o.""InitiatorId"", o.""BakerFee"",
                            o.""GasLimit"", o.""GasUsed"", o.""StorageLimit"", o.""Status"", o.""Amount"", o.""PrevDelegateId"", o.""DelegateId"", o.""Errors"", b.""Hash""
                FROM        ""DelegationOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                WHERE       o.""OpHash"" = @hash::character(51) AND o.""Counter"" = @counter AND o.""Nonce"" = @nonce
                LIMIT       1";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash, counter, nonce });

            return rows.Select(row => new DelegationOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Hash = hash,
                Initiator = row.InitiatorId != null ? Accounts.GetAlias(row.InitiatorId) : null,
                Sender = Accounts.GetAlias(row.SenderId),
                SenderCodeHash = row.SenderCodeHash,
                Counter = counter,
                Nonce = nonce,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                StorageLimit = row.StorageLimit,
                BakerFee = row.BakerFee,
                Amount = row.Amount,
                PrevDelegate = row.PrevDelegateId != null ? Accounts.GetAlias(row.PrevDelegateId) : null,
                NewDelegate = row.DelegateId != null ? Accounts.GetAlias(row.DelegateId) : null,
                Status = OpStatuses.ToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<IEnumerable<DelegationOperation>> GetDelegations(Block block, Symbols quote)
        {
            var sql = @"
                SELECT    ""Id"", ""Timestamp"", ""OpHash"", ""SenderId"", ""InitiatorId"", ""Counter"", ""BakerFee"",
                          ""GasLimit"", ""GasUsed"", ""StorageLimit"", ""Status"", ""Nonce"", ""Amount"", ""PrevDelegateId"", ""DelegateId"", ""Errors""
                FROM      ""DelegationOps""
                WHERE     ""Level"" = @level
                ORDER BY  ""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { level = block.Level });

            return rows.Select(row => new DelegationOperation
            {
                Id = row.Id,
                Level = block.Level,
                Block = block.Hash,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Initiator = row.InitiatorId != null ? Accounts.GetAlias(row.InitiatorId) : null,
                Sender = Accounts.GetAlias(row.SenderId),
                SenderCodeHash = row.SenderCodeHash,
                Counter = row.Counter,
                Nonce = row.Nonce,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                StorageLimit = row.StorageLimit,
                BakerFee = row.BakerFee,
                Amount = row.Amount,
                PrevDelegate = row.PrevDelegateId != null ? Accounts.GetAlias(row.PrevDelegateId) : null,
                NewDelegate = row.DelegateId != null ? Accounts.GetAlias(row.DelegateId) : null,
                Status = OpStatuses.ToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                Quote = Quotes.Get(quote, block.Level)
            });
        }

        public async Task<IEnumerable<DelegationOperation>> GetDelegations(
            AnyOfParameter anyof,
            AccountParameter initiator,
            AccountParameter sender,
            AccountParameter prevDelegate,
            AccountParameter newDelegate,
            Int32Parameter level,
            DateTimeParameter timestamp,
            Int32Parameter senderCodeHash,
            OperationStatusParameter status,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            Symbols quote)
        {
            var sql = new SqlBuilder(@"SELECT o.*, b.""Hash"" FROM ""DelegationOps"" AS o INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""")
                .Filter(anyof, x => x switch
                {
                    "initiator" => "InitiatorId",
                    "sender" => "SenderId",
                    "prevDelegate" => "PrevDelegateId",
                    _ => "DelegateId"
                })
                .Filter("InitiatorId", initiator, x => x == "prevDelegate" ? "PrevDelegateId" : "DelegateId")
                .Filter("SenderId", sender, x => x == "prevDelegate" ? "PrevDelegateId" : "DelegateId")
                .Filter("PrevDelegateId", prevDelegate, x => x == "initiator" ? "InitiatorId" : x == "sender" ? "SenderId" : "DelegateId")
                .Filter("DelegateId", newDelegate, x => x == "initiator" ? "InitiatorId" : x == "sender" ? "SenderId" : "PrevDelegateId")
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Timestamp""", timestamp)
                .FilterA(@"o.""SenderCodeHash""", senderCodeHash)
                .Filter("Status", status)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Id", "Level"),
                    "gasUsed" => ("GasUsed", "GasUsed"),
                    "bakerFee" => ("BakerFee", "BakerFee"),
                    _ => ("Id", "Id")
                }, "o");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row => new DelegationOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Initiator = row.InitiatorId != null ? Accounts.GetAlias(row.InitiatorId) : null,
                Sender = Accounts.GetAlias(row.SenderId),
                SenderCodeHash = row.SenderCodeHash,
                Counter = row.Counter,
                Nonce = row.Nonce,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                StorageLimit = row.StorageLimit,
                BakerFee = row.BakerFee,
                Amount = row.Amount,
                PrevDelegate = row.PrevDelegateId != null ? Accounts.GetAlias(row.PrevDelegateId) : null,
                NewDelegate = row.DelegateId != null ? Accounts.GetAlias(row.DelegateId) : null,
                Status = OpStatuses.ToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<object[][]> GetDelegations(
            AnyOfParameter anyof,
            AccountParameter initiator,
            AccountParameter sender,
            AccountParameter prevDelegate,
            AccountParameter newDelegate,
            Int32Parameter level,
            DateTimeParameter timestamp,
            Int32Parameter senderCodeHash,
            OperationStatusParameter status,
            SortParameter sort,
            OffsetParameter offset,
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
                    case "hash": columns.Add(@"o.""OpHash"""); break;
                    case "initiator": columns.Add(@"o.""InitiatorId"""); break;
                    case "sender": columns.Add(@"o.""SenderId"""); break;
                    case "senderCodeHash": columns.Add(@"o.""SenderCodeHash"""); break;
                    case "counter": columns.Add(@"o.""Counter"""); break;
                    case "nonce": columns.Add(@"o.""Nonce"""); break;
                    case "gasLimit": columns.Add(@"o.""GasLimit"""); break;
                    case "gasUsed": columns.Add(@"o.""GasUsed"""); break;
                    case "storageLimit": columns.Add(@"o.""StorageLimit"""); break;
                    case "bakerFee": columns.Add(@"o.""BakerFee"""); break;
                    case "amount": columns.Add(@"o.""Amount"""); break;
                    case "prevDelegate": columns.Add(@"o.""PrevDelegateId"""); break;
                    case "newDelegate": columns.Add(@"o.""DelegateId"""); break;
                    case "status": columns.Add(@"o.""Status"""); break;
                    case "errors": columns.Add(@"o.""Errors"""); break;
                    case "block":
                        columns.Add(@"b.""Hash""");
                        joins.Add(@"INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""");
                        break;
                    case "quote": columns.Add(@"o.""Level"""); break;
                }
            }

            if (columns.Count == 0)
                return Array.Empty<object[]>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""DelegationOps"" as o {string.Join(' ', joins)}")
                .Filter(anyof, x => x switch
                {
                    "initiator" => "InitiatorId",
                    "sender" => "SenderId",
                    "prevDelegate" => "PrevDelegateId",
                    _ => "DelegateId"
                })
                .Filter("InitiatorId", initiator, x => x == "prevDelegate" ? "PrevDelegateId" : "DelegateId")
                .Filter("SenderId", sender, x => x == "prevDelegate" ? "PrevDelegateId" : "DelegateId")
                .Filter("PrevDelegateId", prevDelegate, x => x == "initiator" ? "InitiatorId" : x == "sender" ? "SenderId" : "DelegateId")
                .Filter("DelegateId", newDelegate, x => x == "initiator" ? "InitiatorId" : x == "sender" ? "SenderId" : "PrevDelegateId")
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Timestamp""", timestamp)
                .FilterA(@"o.""SenderCodeHash""", senderCodeHash)
                .Filter("Status", status)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Id", "Level"),
                    "gasUsed" => ("GasUsed", "GasUsed"),
                    "bakerFee" => ("BakerFee", "BakerFee"),
                    _ => ("Id", "Id")
                }, "o");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            var result = new object[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object[fields.Length];

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
                    case "hash":
                        foreach (var row in rows)
                            result[j++][i] = row.OpHash;
                        break;
                    case "initiator":
                        foreach (var row in rows)
                            result[j++][i] = row.InitiatorId != null ? await Accounts.GetAliasAsync(row.InitiatorId) : null;
                        break;
                    case "sender":
                        foreach (var row in rows)
                            result[j++][i] = await Accounts.GetAliasAsync(row.SenderId);
                        break;
                    case "senderCodeHash":
                        foreach (var row in rows)
                            result[j++][i] = row.SenderCodeHash;
                        break;
                    case "counter":
                        foreach (var row in rows)
                            result[j++][i] = row.Counter;
                        break;
                    case "nonce":
                        foreach (var row in rows)
                            result[j++][i] = row.Nonce;
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
                    case "amount":
                        foreach (var row in rows)
                            result[j++][i] = row.Amount;
                        break;
                    case "prevDelegate":
                        foreach (var row in rows)
                            result[j++][i] = row.PrevDelegateId != null ? await Accounts.GetAliasAsync(row.PrevDelegateId) : null;
                        break;
                    case "newDelegate":
                        foreach (var row in rows)
                            result[j++][i] = row.DelegateId != null ? await Accounts.GetAliasAsync(row.DelegateId) : null;
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

        public async Task<object[]> GetDelegations(
            AnyOfParameter anyof,
            AccountParameter initiator,
            AccountParameter sender,
            AccountParameter prevDelegate,
            AccountParameter newDelegate,
            Int32Parameter level,
            DateTimeParameter timestamp,
            Int32Parameter senderCodeHash,
            OperationStatusParameter status,
            SortParameter sort,
            OffsetParameter offset,
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
                case "hash": columns.Add(@"o.""OpHash"""); break;
                case "initiator": columns.Add(@"o.""InitiatorId"""); break;
                case "sender": columns.Add(@"o.""SenderId"""); break;
                case "senderCodeHash": columns.Add(@"o.""SenderCodeHash"""); break;
                case "counter": columns.Add(@"o.""Counter"""); break;
                case "nonce": columns.Add(@"o.""Nonce"""); break;
                case "gasLimit": columns.Add(@"o.""GasLimit"""); break;
                case "gasUsed": columns.Add(@"o.""GasUsed"""); break;
                case "storageLimit": columns.Add(@"o.""StorageLimit"""); break;
                case "bakerFee": columns.Add(@"o.""BakerFee"""); break;
                case "amount": columns.Add(@"o.""Amount"""); break;
                case "prevDelegate": columns.Add(@"o.""PrevDelegateId"""); break;
                case "newDelegate": columns.Add(@"o.""DelegateId"""); break;
                case "status": columns.Add(@"o.""Status"""); break;
                case "errors": columns.Add(@"o.""Errors"""); break;
                case "block":
                    columns.Add(@"b.""Hash""");
                    joins.Add(@"INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""");
                    break;
                case "quote": columns.Add(@"o.""Level"""); break;
            }

            if (columns.Count == 0)
                return Array.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""DelegationOps"" as o {string.Join(' ', joins)}")
                .Filter(anyof, x => x switch
                {
                    "initiator" => "InitiatorId",
                    "sender" => "SenderId",
                    "prevDelegate" => "PrevDelegateId",
                    _ => "DelegateId"
                })
                .Filter("InitiatorId", initiator, x => x == "prevDelegate" ? "PrevDelegateId" : "DelegateId")
                .Filter("SenderId", sender, x => x == "prevDelegate" ? "PrevDelegateId" : "DelegateId")
                .Filter("PrevDelegateId", prevDelegate, x => x == "initiator" ? "InitiatorId" : x == "sender" ? "SenderId" : "DelegateId")
                .Filter("DelegateId", newDelegate, x => x == "initiator" ? "InitiatorId" : x == "sender" ? "SenderId" : "PrevDelegateId")
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Timestamp""", timestamp)
                .FilterA(@"o.""SenderCodeHash""", senderCodeHash)
                .Filter("Status", status)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Id", "Level"),
                    "gasUsed" => ("GasUsed", "GasUsed"),
                    "bakerFee" => ("BakerFee", "BakerFee"),
                    _ => ("Id", "Id")
                }, "o");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            //TODO: optimize memory allocation
            var result = new object[rows.Count()];
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
                case "hash":
                    foreach (var row in rows)
                        result[j++] = row.OpHash;
                    break;
                case "initiator":
                    foreach (var row in rows)
                        result[j++] = row.InitiatorId != null ? await Accounts.GetAliasAsync(row.InitiatorId) : null;
                    break;
                case "sender":
                    foreach (var row in rows)
                        result[j++] = await Accounts.GetAliasAsync(row.SenderId);
                    break;
                case "senderCodeHash":
                    foreach (var row in rows)
                        result[j++] = row.SenderCodeHash;
                    break;
                case "counter":
                    foreach (var row in rows)
                        result[j++] = row.Counter;
                    break;
                case "nonce":
                    foreach (var row in rows)
                        result[j++] = row.Nonce;
                    break;
                case "gasLimit":
                    foreach (var row in rows)
                        result[j++] = row.GasLimit;
                    break;
                case "gasUsed":
                    foreach (var row in rows)
                        result[j++] = row.GasUsed;
                    break;
                case "storageLimit":
                    foreach (var row in rows)
                        result[j++] = row.StorageLimit;
                    break;
                case "bakerFee":
                    foreach (var row in rows)
                        result[j++] = row.BakerFee;
                    break;
                case "amount":
                    foreach (var row in rows)
                        result[j++] = row.Amount;
                    break;
                case "prevDelegate":
                    foreach (var row in rows)
                        result[j++] = row.PrevDelegateId != null ? await Accounts.GetAliasAsync(row.PrevDelegateId) : null;
                    break;
                case "newDelegate":
                    foreach (var row in rows)
                        result[j++] = row.DelegateId != null ? await Accounts.GetAliasAsync(row.DelegateId) : null;
                    break;
                case "status":
                    foreach (var row in rows)
                        result[j++] = OpStatuses.ToString(row.Status);
                    break;
                case "errors":
                    foreach (var row in rows)
                        result[j++] = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null;
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
