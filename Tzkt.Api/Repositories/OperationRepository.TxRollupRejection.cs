﻿using Dapper;
using Tzkt.Api.Models;
using Tzkt.Data;

namespace Tzkt.Api.Repositories
{
    public partial class OperationRepository
    {
        public async Task<bool?> GetTxRollupRejectionStatus(string hash)
        {
            await using var db = await DataSource.OpenConnectionAsync();
            return await GetStatus(db, nameof(TzktContext.TxRollupRejectionOps), hash);
        }

        public async Task<int> GetTxRollupRejectionOpsCount(
            Int32Parameter? level,
            TimestampParameter? timestamp)
        {
            var sql = new SqlBuilder(@"SELECT COUNT(*) FROM ""TxRollupRejectionOps""")
                .Filter("Level", level)
                .Filter("Level", timestamp);

            await using var db = await DataSource.OpenConnectionAsync();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<TxRollupRejectionOperation>> GetTxRollupRejectionOps(string hash, Symbols quote)
        {
            var sql = @"
                SELECT      o.*, b.""Hash""
                FROM        ""TxRollupRejectionOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                WHERE       o.""OpHash"" = @hash::character(51)
                ORDER BY    o.""Id""";

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql, new { hash });

            return rows.Select(row => new TxRollupRejectionOperation
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
                BakerFee = row.BakerFee,
                Rollup = Accounts.GetAlias(row.RollupId),
                Committer = Accounts.GetAlias(row.CommitterId),
                Reward = row.Reward,
                Loss = row.Loss,
                Status = OpStatuses.ToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<IEnumerable<TxRollupRejectionOperation>> GetTxRollupRejectionOps(string hash, int counter, Symbols quote)
        {
            var sql = @"
                SELECT      o.*, b.""Hash""
                FROM        ""TxRollupRejectionOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                WHERE       o.""OpHash"" = @hash::character(51) AND o.""Counter"" = @counter
                ORDER BY    o.""Id""";

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql, new { hash, counter });

            return rows.Select(row => new TxRollupRejectionOperation
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
                BakerFee = row.BakerFee,
                Rollup = Accounts.GetAlias(row.RollupId),
                Committer = Accounts.GetAlias(row.CommitterId),
                Reward = row.Reward,
                Loss = row.Loss,
                Status = OpStatuses.ToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<IEnumerable<TxRollupRejectionOperation>> GetTxRollupRejectionOps(Block block, Symbols quote)
        {
            var sql = @"
                SELECT      o.*
                FROM        ""TxRollupRejectionOps"" as o
                WHERE       o.""Level"" = @level
                ORDER BY    o.""Id""";

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql, new { level = block.Level });

            return rows.Select(row => new TxRollupRejectionOperation
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
                BakerFee = row.BakerFee,
                Rollup = Accounts.GetAlias(row.RollupId),
                Committer = Accounts.GetAlias(row.CommitterId),
                Reward = row.Reward,
                Loss = row.Loss,
                Status = OpStatuses.ToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<IEnumerable<TxRollupRejectionOperation>> GetTxRollupRejectionOps(
            AnyOfParameter? anyof,
            AccountParameter? sender,
            AccountParameter? committer,
            AccountParameter? rollup,
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
                FROM        ""TxRollupRejectionOps"" AS o
                INNER JOIN  ""Blocks"" as b
                        ON  b.""Level"" = o.""Level""")
                .Filter(anyof, x => x == "sender" ? "SenderId" : "CommitterId")
                .Filter("SenderId", sender)
                .Filter("CommitterId", committer)
                .Filter("RollupId", rollup)
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

            return rows.Select(row => new TxRollupRejectionOperation
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
                BakerFee = row.BakerFee,
                Rollup = Accounts.GetAlias(row.RollupId),
                Committer = Accounts.GetAlias(row.CommitterId),
                Reward = row.Reward,
                Loss = row.Loss,
                Status = OpStatuses.ToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<object?[][]> GetTxRollupRejectionOps(
            AnyOfParameter? anyof,
            AccountParameter? sender,
            AccountParameter? committer,
            AccountParameter? rollup,
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
                    case "bakerFee": columns.Add(@"o.""BakerFee"""); break;
                    case "rollup": columns.Add(@"o.""RollupId"""); break;
                    case "committer": columns.Add(@"o.""CommitterId"""); break;
                    case "reward": columns.Add(@"o.""Reward"""); break;
                    case "loss": columns.Add(@"o.""Loss"""); break;
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
                return [];

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""TxRollupRejectionOps"" as o {string.Join(' ', joins)}")
                .Filter(anyof, x => x == "sender" ? "SenderId" : "CommitterId")
                .Filter("SenderId", sender)
                .Filter("CommitterId", committer)
                .Filter("RollupId", rollup)
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
                    case "bakerFee":
                        foreach (var row in rows)
                            result[j++][i] = row.BakerFee;
                        break;
                    case "rollup":
                        foreach (var row in rows)
                            result[j++][i] = await Accounts.GetAliasAsync(row.RollupId);
                        break;
                    case "committer":
                        foreach (var row in rows)
                            result[j++][i] = await Accounts.GetAliasAsync(row.CommitterId);
                        break;
                    case "reward":
                        foreach (var row in rows)
                            result[j++][i] = row.Reward;
                        break;
                    case "loss":
                        foreach (var row in rows)
                            result[j++][i] = row.Loss;
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

        public async Task<object?[]> GetTxRollupRejectionOps(
            AnyOfParameter? anyof,
            AccountParameter? sender,
            AccountParameter? committer,
            AccountParameter? rollup,
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
                case "bakerFee": columns.Add(@"o.""BakerFee"""); break;
                case "rollup": columns.Add(@"o.""RollupId"""); break;
                case "committer": columns.Add(@"o.""CommitterId"""); break;
                case "reward": columns.Add(@"o.""Reward"""); break;
                case "loss": columns.Add(@"o.""Loss"""); break;
                case "status": columns.Add(@"o.""Status"""); break;
                case "errors": columns.Add(@"o.""Errors"""); break;
                case "block":
                    columns.Add(@"b.""Hash""");
                    joins.Add(@"INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""");
                    break;
                case "quote": columns.Add(@"o.""Level"""); break;
            }

            if (columns.Count == 0)
                return [];

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""TxRollupRejectionOps"" as o {string.Join(' ', joins)}")
                .Filter(anyof, x => x == "sender" ? "SenderId" : "CommitterId")
                .Filter("SenderId", sender)
                .Filter("CommitterId", committer)
                .Filter("RollupId", rollup)
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
                case "bakerFee":
                    foreach (var row in rows)
                        result[j++] = row.BakerFee;
                    break;
                case "rollup":
                    foreach (var row in rows)
                        result[j++] = await Accounts.GetAliasAsync(row.RollupId);
                    break;
                case "committer":
                    foreach (var row in rows)
                        result[j++] = await Accounts.GetAliasAsync(row.CommitterId);
                    break;
                case "reward":
                    foreach (var row in rows)
                        result[j++] = row.Reward;
                    break;
                case "loss":
                    foreach (var row in rows)
                        result[j++] = row.Loss;
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
