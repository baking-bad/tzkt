﻿using Dapper;
using Tzkt.Api.Models;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Repositories
{
    public partial class OperationRepository
    {
        public async Task<int> GetIncreasePaidStorageOpsCount(
            Int32Parameter? level,
            TimestampParameter? timestamp)
        {
            var sql = new SqlBuilder(@"SELECT COUNT(*) FROM ""IncreasePaidStorageOps""")
                .Filter("Level", level)
                .Filter("Level", timestamp);

            await using var db = await DataSource.OpenConnectionAsync();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<IncreasePaidStorageOperation>> GetIncreasePaidStorageOps(string hash, Symbols quote)
        {
            var sql = @"
                SELECT      o.*, b.""Hash""
                FROM        ""IncreasePaidStorageOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                WHERE       o.""OpHash"" = @hash::character(51)
                ORDER BY    o.""Id""";

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql, new { hash });

            return rows.Select(row => new IncreasePaidStorageOperation
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
                Status = OpStatuses.ToString(row.Status),
                Contract = row.ContractId == null ? null : Accounts.GetAlias(row.ContractId),
                Amount = row.Amount,
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<IEnumerable<IncreasePaidStorageOperation>> GetIncreasePaidStorageOps(string hash, int counter, Symbols quote)
        {
            var sql = @"
                SELECT      o.*, b.""Hash""
                FROM        ""IncreasePaidStorageOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                WHERE       o.""OpHash"" = @hash::character(51) AND o.""Counter"" = @counter
                ORDER BY    o.""Id""";

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql, new { hash, counter });

            return rows.Select(row => new IncreasePaidStorageOperation
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
                Status = OpStatuses.ToString(row.Status),
                Contract = row.ContractId == null ? null : Accounts.GetAlias(row.ContractId),
                Amount = row.Amount,
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<IEnumerable<IncreasePaidStorageOperation>> GetIncreasePaidStorageOps(Block block, Symbols quote)
        {
            var sql = @"
                SELECT      o.*
                FROM        ""IncreasePaidStorageOps"" as o
                WHERE       o.""Level"" = @level
                ORDER BY    o.""Id""";

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql, new { level = block.Level });

            return rows.Select(row => new IncreasePaidStorageOperation
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
                Status = OpStatuses.ToString(row.Status),
                Contract = row.ContractId == null ? null : Accounts.GetAlias(row.ContractId),
                Amount = row.Amount,
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<IEnumerable<Activity>> GetIncreasePaidStorageOpsActivity(
            List<RawAccount> accounts,
            ActivityRole roles,
            TimestampParameter? timestamp,
            Pagination pagination,
            Symbols quote)
        {
            List<int>? senderIds = null;
            List<int>? contractIds = null;

            foreach (var account in accounts)
            {
                if (account.IncreasePaidStorageCount == 0)
                    continue;

                if (account is RawUser && (roles & ActivityRole.Sender) != 0)
                {
                    senderIds ??= new(accounts.Count);
                    senderIds.Add(account.Id);
                }
                else if (account is RawContract && (roles & ActivityRole.Target) != 0)
                {
                    contractIds ??= new(accounts.Count);
                    contractIds.Add(account.Id);
                }
            }

            if (senderIds == null && contractIds == null)
                return [];

            var or = new OrParameter(
                ("SenderId", senderIds),
                ("ContractId", contractIds));

            return await GetIncreasePaidStorageOps(
                or,
                null, null, null,
                timestamp,
                null,
                pagination.sort,
                pagination.offset,
                pagination.limit,
                quote);
        }

        public async Task<IEnumerable<IncreasePaidStorageOperation>> GetIncreasePaidStorageOps(
            OrParameter? or,
            AccountParameter? sender,
            AccountParameter? contract,
            Int32Parameter? level,
            TimestampParameter? timestamp,
            OperationStatusParameter? status,
            SortParameter? sort,
            OffsetParameter? offset,
            int limit,
            Symbols quote)
        {
            var sql = new SqlBuilder(@"
                SELECT      o.*, b.""Hash""
                FROM        ""IncreasePaidStorageOps"" AS o
                INNER JOIN  ""Blocks"" as b
                        ON  b.""Level"" = o.""Level""")
                .Filter(or)
                .Filter("SenderId", sender)
                .Filter("ContractId", contract)
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Level""", timestamp)
                .Filter("Status", status)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Level", "Level"),
                    "gasUsed" => ("GasUsed", "GasUsed"),
                    "storageUsed" => ("StorageUsed", "StorageUsed"),
                    "bakerFee" => ("BakerFee", "BakerFee"),
                    "storageFee" => ("StorageFee", "StorageFee"),
                    _ => ("Id", "Id")
                }, "o");

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row => new IncreasePaidStorageOperation
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
                Status = OpStatuses.ToString(row.Status),
                Contract = row.ContractId == null ? null : Accounts.GetAlias(row.ContractId),
                Amount = row.Amount,
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<object?[][]> GetIncreasePaidStorageOps(
            AccountParameter? sender,
            AccountParameter? contract,
            Int32Parameter? level,
            TimestampParameter? timestamp,
            OperationStatusParameter? status,
            SortParameter? sort,
            OffsetParameter? offset,
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
                    case "sender": columns.Add(@"o.""SenderId"""); break;
                    case "counter": columns.Add(@"o.""Counter"""); break;
                    case "gasLimit": columns.Add(@"o.""GasLimit"""); break;
                    case "gasUsed": columns.Add(@"o.""GasUsed"""); break;
                    case "storageLimit": columns.Add(@"o.""StorageLimit"""); break;
                    case "storageUsed": columns.Add(@"o.""StorageUsed"""); break;
                    case "bakerFee": columns.Add(@"o.""BakerFee"""); break;
                    case "storageFee": columns.Add(@"o.""StorageFee"""); break;
                    case "status": columns.Add(@"o.""Status"""); break;
                    case "contract": columns.Add(@"o.""ContractId"""); break;
                    case "amount": columns.Add(@"o.""Amount"""); break;
                    case "errors": columns.Add(@"o.""Errors"""); break;
                    case "block":
                        columns.Add(@"b.""Hash""");
                        joins.Add(@"INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""");
                        break;
                    case "quote": columns.Add(@"o.""Level"""); break;
                }
            }

            if (columns.Count == 0)
                return [];

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""IncreasePaidStorageOps"" as o {string.Join(' ', joins)}")
                .Filter("SenderId", sender)
                .Filter("ContractId", contract)
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Level""", timestamp)
                .Filter("Status", status)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Level", "Level"),
                    "gasUsed" => ("GasUsed", "GasUsed"),
                    "storageUsed" => ("StorageUsed", "StorageUsed"),
                    "bakerFee" => ("BakerFee", "BakerFee"),
                    "storageFee" => ("StorageFee", "StorageFee"),
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
                    case "status":
                        foreach (var row in rows)
                            result[j++][i] = OpStatuses.ToString(row.Status);
                        break;
                    case "contract":
                        foreach (var row in rows)
                            result[j++][i] = row.ContractId == null ? null : Accounts.GetAlias(row.ContractId);
                        break;
                    case "amount":
                        foreach (var row in rows)
                            result[j++][i] = row.Amount;
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

        public async Task<object?[]> GetIncreasePaidStorageOps(
            AccountParameter? sender,
            AccountParameter? contract,
            Int32Parameter? level,
            TimestampParameter? timestamp,
            OperationStatusParameter? status,
            SortParameter? sort,
            OffsetParameter? offset,
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
                case "sender": columns.Add(@"o.""SenderId"""); break;
                case "counter": columns.Add(@"o.""Counter"""); break;
                case "gasLimit": columns.Add(@"o.""GasLimit"""); break;
                case "gasUsed": columns.Add(@"o.""GasUsed"""); break;
                case "storageLimit": columns.Add(@"o.""StorageLimit"""); break;
                case "storageUsed": columns.Add(@"o.""StorageUsed"""); break;
                case "bakerFee": columns.Add(@"o.""BakerFee"""); break;
                case "storageFee": columns.Add(@"o.""StorageFee"""); break;
                case "status": columns.Add(@"o.""Status"""); break;
                case "contract": columns.Add(@"o.""ContractId"""); break;
                case "amount": columns.Add(@"o.""Amount"""); break;
                case "errors": columns.Add(@"o.""Errors"""); break;
                case "block":
                    columns.Add(@"b.""Hash""");
                    joins.Add(@"INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""");
                    break;
                case "quote": columns.Add(@"o.""Level"""); break;
            }

            if (columns.Count == 0)
                return [];

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""IncreasePaidStorageOps"" as o {string.Join(' ', joins)}")
                .Filter("SenderId", sender)
                .Filter("ContractId", contract)
                .FilterA(@"o.""Level""", level)
                .FilterA(@"o.""Level""", timestamp)
                .Filter("Status", status)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Level", "Level"),
                    "gasUsed" => ("GasUsed", "GasUsed"),
                    "storageUsed" => ("StorageUsed", "StorageUsed"),
                    "bakerFee" => ("BakerFee", "BakerFee"),
                    "storageFee" => ("StorageFee", "StorageFee"),
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
                case "status":
                    foreach (var row in rows)
                        result[j++] = OpStatuses.ToString(row.Status);
                    break;
                case "contract":
                    foreach (var row in rows)
                        result[j++] = row.ContractId == null ? null : Accounts.GetAlias(row.ContractId);
                    break;
                case "amount":
                    foreach (var row in rows)
                        result[j++] = row.Amount;
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
