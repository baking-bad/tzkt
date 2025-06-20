﻿using Dapper;
using Netezos.Encoding;
using Tzkt.Api.Models;
using Tzkt.Api.Services.Cache;
using Tzkt.Data;

namespace Tzkt.Api.Repositories
{
    public partial class OperationRepository
    {
        public async Task<bool?> GetTransferTicketStatus(string hash)
        {
            await using var db = await DataSource.OpenConnectionAsync();
            return await GetStatus(db, nameof(TzktContext.TransferTicketOps), hash);
        }

        public async Task<int> GetTransferTicketOpsCount(
            Int32Parameter? level,
            TimestampParameter? timestamp)
        {
            var sql = new SqlBuilder(@"SELECT COUNT(*) FROM ""TransferTicketOps""")
                .Filter("Level", level)
                .Filter("Level", timestamp);

            await using var db = await DataSource.OpenConnectionAsync();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<TransferTicketOperation>> GetTransferTicketOps(string hash, MichelineFormat format, Symbols quote)
        {
            var sql = @"
                SELECT      o.*, b.""Hash""
                FROM        ""TransferTicketOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                WHERE       o.""OpHash"" = @hash::character(51)
                ORDER BY    o.""Id""";

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql, new { hash });

            return rows.Select(row => new TransferTicketOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Sender = Accounts.GetAlias(row.SenderId),
                Counter = row.Counter,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                StorageLimit = row.StorageLimit,
                StorageUsed = row.StorageUsed,
                BakerFee = row.BakerFee,
                StorageFee = row.StorageFee ?? 0,
                Target = row.TargetId == null ? null : Accounts.GetAlias(row.TargetId) ,
                Ticketer = row.TicketerId == null ? null : Accounts.GetAlias(row.TicketerId),
                Amount = row.Amount,
                Entrypoint = row.Entrypoint,
                ContentType = (RawJson)Micheline.ToJson(row.RawType),
                Content = format switch
                {
                    MichelineFormat.Json => row.JsonContent == null ? null : (RawJson)row.JsonContent,
                    MichelineFormat.JsonString => row.JsonContent,
                    MichelineFormat.Raw => row.RawContent == null ? null : (RawJson)Micheline.ToJson(row.RawContent),
                    MichelineFormat.RawString => row.RawContent == null ? null : Micheline.ToJson(row.RawContent),
                    _ => throw new Exception("Invalid MichelineFormat value")
                },
                Status = OpStatuses.ToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                TicketTransfersCount = row.TicketTransfers,
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<IEnumerable<TransferTicketOperation>> GetTransferTicketOps(string hash, int counter, MichelineFormat format, Symbols quote)
        {
            var sql = @"
                SELECT      o.*, b.""Hash""
                FROM        ""TransferTicketOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                WHERE       o.""OpHash"" = @hash::character(51) AND o.""Counter"" = @counter
                ORDER BY    o.""Id""";

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql, new { hash, counter });

            return rows.Select(row => new TransferTicketOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Sender = Accounts.GetAlias(row.SenderId),
                Counter = row.Counter,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                StorageLimit = row.StorageLimit,
                StorageUsed = row.StorageUsed,
                BakerFee = row.BakerFee,
                StorageFee = row.StorageFee ?? 0,
                Target = row.TargetId == null ? null : Accounts.GetAlias(row.TargetId),
                Ticketer = row.TicketerId == null ? null : Accounts.GetAlias(row.TicketerId),
                Amount = row.Amount,
                Entrypoint = row.Entrypoint,
                ContentType = (RawJson)Micheline.ToJson(row.RawType),
                Content = format switch
                {
                    MichelineFormat.Json => row.JsonContent == null ? null : (RawJson)row.JsonContent,
                    MichelineFormat.JsonString => row.JsonContent,
                    MichelineFormat.Raw => row.RawContent == null ? null : (RawJson)Micheline.ToJson(row.RawContent),
                    MichelineFormat.RawString => row.RawContent == null ? null : Micheline.ToJson(row.RawContent),
                    _ => throw new Exception("Invalid MichelineFormat value")
                },
                Status = OpStatuses.ToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                TicketTransfersCount = row.TicketTransfers,
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<IEnumerable<TransferTicketOperation>> GetTransferTicketOps(Block block, MichelineFormat format, Symbols quote)
        {
            var sql = @"
                SELECT      o.*
                FROM        ""TransferTicketOps"" as o
                WHERE       o.""Level"" = @level
                ORDER BY    o.""Id""";

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql, new { level = block.Level });

            return rows.Select(row => new TransferTicketOperation
            {
                Id = row.Id,
                Level = block.Level,
                Block = block.Hash,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Sender = Accounts.GetAlias(row.SenderId),
                Counter = row.Counter,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                StorageLimit = row.StorageLimit,
                StorageUsed = row.StorageUsed,
                BakerFee = row.BakerFee,
                StorageFee = row.StorageFee ?? 0,
                Target = row.TargetId == null ? null : Accounts.GetAlias(row.TargetId),
                Ticketer = row.TicketerId == null ? null : Accounts.GetAlias(row.TicketerId),
                Amount = row.Amount,
                Entrypoint = row.Entrypoint,
                ContentType = (RawJson)Micheline.ToJson(row.RawType),
                Content = format switch
                {
                    MichelineFormat.Json => row.JsonContent == null ? null : (RawJson)row.JsonContent,
                    MichelineFormat.JsonString => row.JsonContent,
                    MichelineFormat.Raw => row.RawContent == null ? null : (RawJson)Micheline.ToJson(row.RawContent),
                    MichelineFormat.RawString => row.RawContent == null ? null : Micheline.ToJson(row.RawContent),
                    _ => throw new Exception("Invalid MichelineFormat value")
                },
                Status = OpStatuses.ToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                TicketTransfersCount = row.TicketTransfers,
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<IEnumerable<Activity>> GetTransferTicketOpsActivity(
            List<RawAccount> accounts,
            ActivityRole roles,
            TimestampParameter? timestamp,
            Pagination pagination,
            Symbols quote,
            MichelineFormat format)
        {
            List<int>? senderIds = null;
            List<int>? targetIds = null;
            List<int>? ticketerIds = null;

            foreach (var account in accounts)
            {
                if (account.TransferTicketCount == 0)
                    continue;

                if ((roles & ActivityRole.Sender) != 0)
                {
                    senderIds ??= new(accounts.Count);
                    senderIds.Add(account.Id);
                }

                if ((roles & ActivityRole.Target) != 0)
                {
                    targetIds ??= new(accounts.Count);
                    targetIds.Add(account.Id);
                }

                if (account is RawContract && (roles & ActivityRole.Mention) != 0)
                {
                    ticketerIds ??= new(accounts.Count);
                    ticketerIds.Add(account.Id);
                }
            }

            if (senderIds == null && targetIds == null && ticketerIds == null)
                return [];

            var or = new OrParameter(
                ("SenderId", senderIds),
                ("TargetId", targetIds),
                ("TicketerId", ticketerIds));

            return await GetTransferTicketOps(
                or,
                null, null, null, null, null, null,
                timestamp,
                null,
                pagination.sort,
                pagination.offset,
                pagination.limit,
                format,
                quote);
        }

        public async Task<IEnumerable<TransferTicketOperation>> GetTransferTicketOps(
            OrParameter? or,
            AnyOfParameter? anyof,
            AccountParameter? sender,
            AccountParameter? target,
            AccountParameter? ticketer,
            Int64Parameter? id,
            Int32Parameter? level,
            TimestampParameter? timestamp,
            OperationStatusParameter? status,
            SortParameter? sort,
            OffsetParameter? offset,
            int limit,
            MichelineFormat format,
            Symbols quote)
        {
            var sql = new SqlBuilder(@"
                SELECT      o.*, b.""Hash""
                FROM        ""TransferTicketOps"" AS o
                INNER JOIN  ""Blocks"" as b
                        ON  b.""Level"" = o.""Level""")
                .Filter(or)
                .Filter(anyof, x => x switch
                {
                    "sender" => "SenderId",
                    "target" => "TargetId",
                    _ => "TicketerId"
                })
                .Filter("SenderId", sender)
                .Filter("TargetId", target)
                .Filter("TicketerId", ticketer)
                .FilterA(@"o.""Id""", id)
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Level""", timestamp)
                .Filter("Status", status)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Level", "Level"),
                    "gasUsed" => ("GasUsed", "GasUsed"),
                    "bakerFee" => ("BakerFee", "BakerFee"),
                    _ => ("Id", "Id")
                }, "o");

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row => new TransferTicketOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Sender = Accounts.GetAlias(row.SenderId),
                Counter = row.Counter,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                StorageLimit = row.StorageLimit,
                StorageUsed = row.StorageUsed,
                BakerFee = row.BakerFee,
                StorageFee = row.StorageFee ?? 0,
                Target = row.TargetId == null ? null : Accounts.GetAlias(row.TargetId),
                Ticketer = row.TicketerId == null ? null : Accounts.GetAlias(row.TicketerId),
                Amount = row.Amount,
                Entrypoint = row.Entrypoint,
                ContentType = (RawJson)Micheline.ToJson(row.RawType),
                Content = format switch
                {
                    MichelineFormat.Json => row.JsonContent == null ? null : (RawJson)row.JsonContent,
                    MichelineFormat.JsonString => row.JsonContent,
                    MichelineFormat.Raw => row.RawContent == null ? null : (RawJson)Micheline.ToJson(row.RawContent),
                    MichelineFormat.RawString => row.RawContent == null ? null : Micheline.ToJson(row.RawContent),
                    _ => throw new Exception("Invalid MichelineFormat value")
                },
                Status = OpStatuses.ToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                TicketTransfersCount = row.TicketTransfers,
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<object?[][]> GetTransferTicketOps(
            AnyOfParameter? anyof,
            AccountParameter? sender,
            AccountParameter? target,
            AccountParameter? ticketer,
            Int64Parameter? id,
            Int32Parameter? level,
            TimestampParameter? timestamp,
            OperationStatusParameter? status,
            SortParameter? sort,
            OffsetParameter? offset,
            int limit,
            string[] fields,
            MichelineFormat format,
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
                    case "sender": columns.Add(@"o.""SenderId"""); break;
                    case "counter": columns.Add(@"o.""Counter"""); break;
                    case "gasLimit": columns.Add(@"o.""GasLimit"""); break;
                    case "gasUsed": columns.Add(@"o.""GasUsed"""); break;
                    case "storageLimit": columns.Add(@"o.""StorageLimit"""); break;
                    case "storageUsed": columns.Add(@"o.""StorageUsed"""); break;
                    case "bakerFee": columns.Add(@"o.""BakerFee"""); break;
                    case "storageFee": columns.Add(@"o.""StorageFee"""); break;
                    case "target": columns.Add(@"o.""TargetId"""); break;
                    case "ticketer": columns.Add(@"o.""TicketerId"""); break;
                    case "amount": columns.Add(@"o.""Amount"""); break;
                    case "entrypoint": columns.Add(@"o.""Entrypoint"""); break;
                    case "contentType": columns.Add(@"o.""RawType"""); break;
                    case "content":
                        columns.Add(format switch
                        {
                            MichelineFormat.Json => @"o.""JsonContent""",
                            MichelineFormat.JsonString => @"o.""JsonContent""",
                            MichelineFormat.Raw => @"o.""RawContent""",
                            MichelineFormat.RawString => @"o.""RawContent""",
                            _ => throw new Exception("Invalid MichelineFormat value")
                        });
                        break;
                    case "status": columns.Add(@"o.""Status"""); break;
                    case "errors": columns.Add(@"o.""Errors"""); break;
                    case "block":
                        columns.Add(@"b.""Hash""");
                        joins.Add(@"INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""");
                        break;
                    case "ticketTransfersCount": columns.Add(@"o.""TicketTransfers"""); break;
                    case "quote": columns.Add(@"o.""Level"""); break;
                }
            }

            if (columns.Count == 0)
                return [];

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""TransferTicketOps"" as o {string.Join(' ', joins)}")
                .Filter(anyof, x => x switch
                {
                    "sender" => "SenderId",
                    "target" => "TargetId",
                    _ => "TicketerId"
                })
                .Filter("SenderId", sender)
                .Filter("TargetId", target)
                .Filter("TicketerId", ticketer)
                .FilterA(@"o.""Id""", id)
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Level""", timestamp)
                .Filter("Status", status)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Level", "Level"),
                    "gasUsed" => ("GasUsed", "GasUsed"),
                    "bakerFee" => ("BakerFee", "BakerFee"),
                    _ => ("Id", "Id")
                }, "o");

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            var result = new object?[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object?[fields.Length];

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
                    case "sender":
                        foreach (var row in rows)
                            result[j++][i] = await Accounts.GetAliasAsync(row.SenderId);
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
                    case "storageUsed":
                        foreach (var row in rows)
                            result[j++][i] = row.StorageUsed;
                        break;
                    case "bakerFee":
                        foreach (var row in rows)
                            result[j++][i] = row.BakerFee;
                        break;
                    case "storageFee":
                        foreach (var row in rows)
                            result[j++][i] = row.StorageFee ?? 0;
                        break;
                    case "target":
                        foreach (var row in rows)
                            result[j++][i] = row.TargetId == null ? null : await Accounts.GetAliasAsync(row.TargetId);
                        break;
                    case "ticketer":
                        foreach (var row in rows)
                            result[j++][i] = row.TicketerId == null ? null : await Accounts.GetAliasAsync(row.TicketerId);
                        break;
                    case "amount":
                        foreach (var row in rows)
                            result[j++][i] = row.Amount;
                        break;
                    case "entrypoint":
                        foreach (var row in rows)
                            result[j++][i] = row.Entrypoint;
                        break;
                    case "contentType":
                        foreach (var row in rows)
                            result[j++][i] = (RawJson)Micheline.ToJson(row.RawType);
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
                    case "status":
                        foreach (var row in rows)
                            result[j++][i] = OpStatuses.ToString(row.Status);
                        break;
                    case "limit":
                        foreach (var row in rows)
                            result[j++][i] = row.Limit;
                        break;
                    case "errors":
                        foreach (var row in rows)
                            result[j++][i] = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null;
                        break;
                    case "ticketTransfersCount":
                        foreach (var row in rows)
                            result[j++][i] = row.TicketTransfers;
                        break;
                    case "quote":
                        foreach (var row in rows)
                            result[j++][i] = Quotes.Get(quote, row.Level);
                        break;
                }
            }

            return result;
        }

        public async Task<object?[]> GetTransferTicketOps(
            AnyOfParameter? anyof,
            AccountParameter? sender,
            AccountParameter? target,
            AccountParameter? ticketer,
            Int64Parameter? id,
            Int32Parameter? level,
            TimestampParameter? timestamp,
            OperationStatusParameter? status,
            SortParameter? sort,
            OffsetParameter? offset,
            int limit,
            string field,
            MichelineFormat format,
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
                case "sender": columns.Add(@"o.""SenderId"""); break;
                case "counter": columns.Add(@"o.""Counter"""); break;
                case "gasLimit": columns.Add(@"o.""GasLimit"""); break;
                case "gasUsed": columns.Add(@"o.""GasUsed"""); break;
                case "storageLimit": columns.Add(@"o.""StorageLimit"""); break;
                case "storageUsed": columns.Add(@"o.""StorageUsed"""); break;
                case "bakerFee": columns.Add(@"o.""BakerFee"""); break;
                case "storageFee": columns.Add(@"o.""StorageFee"""); break;
                case "target": columns.Add(@"o.""TargetId"""); break;
                case "ticketer": columns.Add(@"o.""TicketerId"""); break;
                case "amount": columns.Add(@"o.""Amount"""); break;
                case "entrypoint": columns.Add(@"o.""Entrypoint"""); break;
                case "contentType": columns.Add(@"o.""RawType"""); break;
                case "content":
                    columns.Add(format switch
                    {
                        MichelineFormat.Json => @"o.""JsonContent""",
                        MichelineFormat.JsonString => @"o.""JsonContent""",
                        MichelineFormat.Raw => @"o.""RawContent""",
                        MichelineFormat.RawString => @"o.""RawContent""",
                        _ => throw new Exception("Invalid MichelineFormat value")
                    });
                    break;
                case "status": columns.Add(@"o.""Status"""); break;
                case "errors": columns.Add(@"o.""Errors"""); break;
                case "block":
                    columns.Add(@"b.""Hash""");
                    joins.Add(@"INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""");
                    break;
                case "ticketTransfersCount": columns.Add(@"o.""TicketTransfers"""); break;
                case "quote": columns.Add(@"o.""Level"""); break;
            }

            if (columns.Count == 0)
                return [];

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""TransferTicketOps"" as o {string.Join(' ', joins)}")
                .Filter(anyof, x => x switch
                {
                    "sender" => "SenderId",
                    "target" => "TargetId",
                    _ => "TicketerId"
                })
                .Filter("SenderId", sender)
                .Filter("TargetId", target)
                .Filter("TicketerId", ticketer)
                .FilterA(@"o.""Id""", id)
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Level""", timestamp)
                .Filter("Status", status)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Level", "Level"),
                    "gasUsed" => ("GasUsed", "GasUsed"),
                    "bakerFee" => ("BakerFee", "BakerFee"),
                    _ => ("Id", "Id")
                }, "o");

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            //TODO: optimize memory allocation
            var result = new object?[rows.Count()];
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
                case "sender":
                    foreach (var row in rows)
                        result[j++] = await Accounts.GetAliasAsync(row.SenderId);
                    break;
                case "counter":
                    foreach (var row in rows)
                        result[j++] = row.Counter;
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
                case "storageUsed":
                    foreach (var row in rows)
                        result[j++] = row.StorageUsed;
                    break;
                case "bakerFee":
                    foreach (var row in rows)
                        result[j++] = row.BakerFee;
                    break;
                case "storageFee":
                    foreach (var row in rows)
                        result[j++] = row.StorageFee ?? 0;
                    break;
                case "target":
                    foreach (var row in rows)
                        result[j++] = row.TargetId == null ? null : await Accounts.GetAliasAsync(row.TargetId);
                    break;
                case "ticketer":
                    foreach (var row in rows)
                        result[j++] = row.TicketerId == null ? null : await Accounts.GetAliasAsync(row.TicketerId);
                    break;
                case "amount":
                    foreach (var row in rows)
                        result[j++] = row.Amount;
                    break;
                case "entrypoint":
                    foreach (var row in rows)
                        result[j++] = row.Entrypoint;
                    break;
                case "contentType":
                    foreach (var row in rows)
                        result[j++] = (RawJson)Micheline.ToJson(row.RawType);
                    break;
                case "content":
                    foreach (var row in rows)
                        result[j++] = format switch
                        {
                            MichelineFormat.Json => row.JsonContent == null ? null : (RawJson)row.JsonContent,
                            MichelineFormat.JsonString => row.JsonContent,
                            MichelineFormat.Raw => row.RawContent == null ? null : (RawJson)Micheline.ToJson(row.RawContent),
                            MichelineFormat.RawString => row.RawContent == null ? null : Micheline.ToJson(row.RawContent),
                            _ => throw new Exception("Invalid MichelineFormat value")
                        };
                    break;
                case "status":
                    foreach (var row in rows)
                        result[j++] = OpStatuses.ToString(row.Status);
                    break;
                case "limit":
                    foreach (var row in rows)
                        result[j++] = row.Limit;
                    break;
                case "errors":
                    foreach (var row in rows)
                        result[j++] = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null;
                    break;
                case "ticketTransfersCount":
                    foreach (var row in rows)
                        result[j++] = row.TicketTransfers;
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
