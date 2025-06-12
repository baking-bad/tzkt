using Dapper;
using Tzkt.Api.Models;
using Tzkt.Api.Services.Cache;
using Tzkt.Data;

namespace Tzkt.Api.Repositories
{
    public partial class OperationRepository
    {
        public async Task<bool?> GetUpdateSecondaryKeyStatus(string hash)
        {
            await using var db = await DataSource.OpenConnectionAsync();
            return await GetStatus(db, nameof(TzktContext.UpdateSecondaryKeyOps), hash);
        }

        public async Task<int> GetUpdateSecondaryKeysCount(
            SecondaryKeyTypeParameter? keyType,
            Int32Parameter? level,
            TimestampParameter? timestamp)
        {
            var sql = new SqlBuilder(@"SELECT COUNT(*) FROM ""UpdateSecondaryKeyOps""")
                .Filter("KeyType", keyType)
                .Filter("Level", level)
                .Filter("Level", timestamp);

            await using var db = await DataSource.OpenConnectionAsync();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<UpdateSecondaryKeyOperation>> GetUpdateSecondaryKeys(string hash, Symbols quote)
        {
            var sql = @"
                SELECT      o.*, b.""Hash""
                FROM        ""UpdateSecondaryKeyOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                WHERE       o.""OpHash"" = @hash::character(51)
                ORDER BY    o.""Id""";

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql, new { hash });

            return rows.Select(row => new UpdateSecondaryKeyOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Hash = hash,
                Sender = Accounts.GetAlias(row.SenderId),
                Counter = row.Counter,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                StorageLimit = row.StorageLimit,
                BakerFee = row.BakerFee,
                Status = OpStatuses.ToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                KeyType = SecondaryKeyTypes.ToString(row.KeyType),
                ActivationCycle = row.ActivationCycle,
                PublicKey = row.PublicKey,
                PublicKeyHash = row.PublicKeyHash,
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<IEnumerable<UpdateSecondaryKeyOperation>> GetUpdateSecondaryKeys(string hash, int counter, Symbols quote)
        {
            var sql = @"
                SELECT      o.*, b.""Hash""
                FROM        ""UpdateSecondaryKeyOps"" as o
                INNER JOIN  ""Blocks"" as b 
                        ON  b.""Level"" = o.""Level""
                WHERE       o.""OpHash"" = @hash::character(51) AND o.""Counter"" = @counter
                LIMIT       1";

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql, new { hash, counter });

            return rows.Select(row => new UpdateSecondaryKeyOperation
            {
                Id = row.Id,
                Level = row.Level,
                Block = row.Hash,
                Timestamp = row.Timestamp,
                Hash = hash,
                Sender = Accounts.GetAlias(row.SenderId),
                Counter = counter,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                StorageLimit = row.StorageLimit,
                BakerFee = row.BakerFee,
                Status = OpStatuses.ToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                KeyType = SecondaryKeyTypes.ToString(row.KeyType),
                ActivationCycle = row.ActivationCycle,
                PublicKey = row.PublicKey,
                PublicKeyHash = row.PublicKeyHash,
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<IEnumerable<UpdateSecondaryKeyOperation>> GetUpdateSecondaryKeys(Block block, Symbols quote)
        {
            var sql = @"
                SELECT    *
                FROM      ""UpdateSecondaryKeyOps""
                WHERE     ""Level"" = @level
                ORDER BY  ""Id""";

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql, new { level = block.Level });

            return rows.Select(row => new UpdateSecondaryKeyOperation
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
                Status = OpStatuses.ToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                KeyType = SecondaryKeyTypes.ToString(row.KeyType),
                ActivationCycle = row.ActivationCycle,
                PublicKey = row.PublicKey,
                PublicKeyHash = row.PublicKeyHash,
                Quote = Quotes.Get(quote, block.Level)
            });
        }

        public async Task<IEnumerable<Activity>> GetUpdateSecondaryKeyOpsActivity(
            List<RawAccount> accounts,
            ActivityRole roles,
            TimestampParameter? timestamp,
            Pagination pagination,
            Symbols quote)
        {
            List<int>? ids = null;

            foreach (var account in accounts)
            {
                if (account.UpdateSecondaryKeyCount == 0)
                    continue;

                if ((roles & ActivityRole.Sender) != 0)
                {
                    ids ??= new(accounts.Count);
                    ids.Add(account.Id);
                }
            }

            if (ids == null)
                return [];

            var or = new OrParameter(("SenderId", ids));

            return await GetUpdateSecondaryKeys(
                or,
                null, null, null, null, null,
                timestamp,
                null,
                pagination.sort,
                pagination.offset,
                pagination.limit,
                quote);
        }

        public async Task<IEnumerable<UpdateSecondaryKeyOperation>> GetUpdateSecondaryKeys(
            OrParameter? or,
            AccountParameter? sender,
            SecondaryKeyTypeParameter? keyType,
            Int32Parameter? activationCycle,
            AddressParameter? publicKeyHash,
            Int32Parameter? level,
            TimestampParameter? timestamp,
            OperationStatusParameter? status,
            SortParameter? sort,
            OffsetParameter? offset,
            int limit,
            Symbols quote)
        {
            var sql = new SqlBuilder(@"SELECT o.*, b.""Hash"" FROM ""UpdateSecondaryKeyOps"" AS o INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""")
                .Filter(or)
                .Filter("SenderId", sender)
                .Filter("KeyType", keyType)
                .Filter("ActivationCycle", activationCycle)
                .Filter("PublicKeyHash", publicKeyHash)
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

            return rows.Select(row => new UpdateSecondaryKeyOperation
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
                Status = OpStatuses.ToString(row.Status),
                Errors = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null,
                KeyType = SecondaryKeyTypes.ToString(row.KeyType),
                ActivationCycle = row.ActivationCycle,
                PublicKey = row.PublicKey,
                PublicKeyHash = row.PublicKeyHash,
                Quote = Quotes.Get(quote, row.Level)
            });
        }

        public async Task<object?[][]> GetUpdateSecondaryKeys(
            AccountParameter? sender,
            SecondaryKeyTypeParameter? keyType,
            Int32Parameter? activationCycle,
            AddressParameter? publicKeyHash,
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
                    case "status": columns.Add(@"o.""Status"""); break;
                    case "errors": columns.Add(@"o.""Errors"""); break;
                    case "keyType": columns.Add(@"o.""KeyType"""); break;
                    case "activationCycle": columns.Add(@"o.""ActivationCycle"""); break;
                    case "publicKey": columns.Add(@"o.""PublicKey"""); break;
                    case "publicKeyHash": columns.Add(@"o.""PublicKeyHash"""); break;
                    case "block":
                        columns.Add(@"b.""Hash""");
                        joins.Add(@"INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""");
                        break;
                    case "quote": columns.Add(@"o.""Level"""); break;
                }
            }

            if (columns.Count == 0)
                return [];

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""UpdateSecondaryKeyOps"" as o {string.Join(' ', joins)}")
                .Filter("SenderId", sender)
                .Filter("KeyType", keyType)
                .Filter("ActivationCycle", activationCycle)
                .Filter("PublicKeyHash", publicKeyHash)
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
                    case "status":
                        foreach (var row in rows)
                            result[j++][i] = OpStatuses.ToString(row.Status);
                        break;
                    case "errors":
                        foreach (var row in rows)
                            result[j++][i] = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null;
                        break;
                    case "keyType":
                        foreach (var row in rows)
                            result[j++][i] = SecondaryKeyTypes.ToString(row.KeyType);
                        break;
                    case "activationCycle":
                        foreach (var row in rows)
                            result[j++][i] = row.ActivationCycle;
                        break;
                    case "publicKey":
                        foreach (var row in rows)
                            result[j++][i] = row.PublicKey;
                        break;
                    case "publicKeyHash":
                        foreach (var row in rows)
                            result[j++][i] = row.PublicKeyHash;
                        break;
                    case "quote":
                        foreach (var row in rows)
                            result[j++][i] = Quotes.Get(quote, row.Level);
                        break;
                }
            }

            return result;
        }

        public async Task<object?[]> GetUpdateSecondaryKeys(
            AccountParameter? sender,
            SecondaryKeyTypeParameter? keyType,
            Int32Parameter? activationCycle,
            AddressParameter? publicKeyHash,
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
                case "status": columns.Add(@"o.""Status"""); break;
                case "errors": columns.Add(@"o.""Errors"""); break;
                case "keyType": columns.Add(@"o.""KeyType"""); break;
                case "activationCycle": columns.Add(@"o.""ActivationCycle"""); break;
                case "publicKey": columns.Add(@"o.""PublicKey"""); break;
                case "publicKeyHash": columns.Add(@"o.""PublicKeyHash"""); break;
                case "block":
                    columns.Add(@"b.""Hash""");
                    joins.Add(@"INNER JOIN ""Blocks"" as b ON b.""Level"" = o.""Level""");
                    break;
                case "quote": columns.Add(@"o.""Level"""); break;
            }

            if (columns.Count == 0)
                return [];

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""UpdateSecondaryKeyOps"" as o {string.Join(' ', joins)}")
                .Filter("SenderId", sender)
                .Filter("KeyType", keyType)
                .Filter("ActivationCycle", activationCycle)
                .Filter("PublicKeyHash", publicKeyHash)
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
                case "status":
                    foreach (var row in rows)
                        result[j++] = OpStatuses.ToString(row.Status);
                    break;
                case "errors":
                    foreach (var row in rows)
                        result[j++] = row.Errors != null ? OperationErrorSerializer.Deserialize(row.Errors) : null;
                    break;
                case "keyType":
                    foreach (var row in rows)
                        result[j++] = SecondaryKeyTypes.ToString(row.KeyType);
                    break;
                case "activationCycle":
                    foreach (var row in rows)
                        result[j++] = row.ActivationCycle;
                    break;
                case "publicKey":
                    foreach (var row in rows)
                        result[j++] = row.PublicKey;
                    break;
                case "publicKeyHash":
                    foreach (var row in rows)
                        result[j++] = row.PublicKeyHash;
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
