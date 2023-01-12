using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Dapper;
using Netezos.Encoding;
using Netezos.Contracts;
using Tzkt.Api.Models;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Repositories
{
    public class BigMapsRepository : DbConnection
    {
        readonly AccountsCache Accounts;
        readonly BigMapsCache BigMaps;
        readonly TimeCache Times;

        public BigMapsRepository(AccountsCache accounts, BigMapsCache bigMaps, TimeCache times, IConfiguration config) : base(config)
        {
            Accounts = accounts;
            BigMaps = bigMaps;
            Times = times;
        }

        #region bigmaps
        public async Task<int> GetCount()
        {
            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(@"SELECT COUNT(*) FROM ""BigMaps""");
        }

        public async Task<MichelinePrim> GetMicheType(int ptr)
        {
            var sql = @"
                SELECT  ""KeyType"", ""ValueType""
                FROM    ""BigMaps""
                WHERE   ""Ptr"" = @ptr
                LIMIT   1";

            using var db = GetConnection();
            var row = await db.QueryFirstOrDefaultAsync(sql, new { ptr });
            if (row == null) return null;

            return new MichelinePrim
            {
                Prim = PrimType.big_map,
                Args = new List<IMicheline>
                {
                    Micheline.FromBytes(row.KeyType),
                    Micheline.FromBytes(row.ValueType),
                }
            };
        }

        public async Task<BigMap> Get(int ptr, MichelineFormat micheline)
        {
            var sql = @"
                SELECT  *
                FROM    ""BigMaps""
                WHERE   ""Ptr"" = @ptr
                LIMIT   1";

            using var db = GetConnection();
            var row = await db.QueryFirstOrDefaultAsync(sql, new { ptr });
            if (row == null) return null;

            return ReadBigMap(row, micheline);
        }

        public async Task<BigMap> Get(int contractId, string path, MichelineFormat micheline)
        {
            var sql = @"
                SELECT  *
                FROM    ""BigMaps""
                WHERE   ""ContractId"" = @id
                AND 	""Active"" = true
                AND     ""StoragePath"" LIKE @path";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { id = contractId, path = $"%{path.Replace("_", "\\_")}" });
            if (!rows.Any()) return null;

            var row = rows.FirstOrDefault(x => x.StoragePath == path);
            return ReadBigMap(row ?? rows.FirstOrDefault(), micheline);
        }

        public Task<int?> GetPtr(int contractId, string path)
        {
            return BigMaps.GetPtrAsync(contractId, path);
        }

        public async Task<IEnumerable<BigMap>> Get(
            AccountParameter contract,
            StringParameter path,
            BigMapTagsParameter tags,
            bool? active,
            Int32Parameter lastLevel,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            MichelineFormat micheline)
        {
            var sql = new SqlBuilder(@"SELECT * FROM ""BigMaps""")
                .Filter("ContractId", contract)
                .Filter("StoragePath", path)
                .Filter("Tags", tags)
                .Filter("Active", active)
                .Filter("LastLevel", lastLevel)
                .Take(sort, offset, limit, x => x switch
                {
                    "ptr" => ("Ptr", "Ptr"),
                    "firstLevel" => ("Id", "FirstLevel"),
                    "lastLevel" => ("LastLevel", "LastLevel"),
                    "totalKeys" => ("TotalKeys", "TotalKeys"),
                    "activeKeys" => ("ActiveKeys", "ActiveKeys"),
                    "updates" => ("Updates", "Updates"),
                    _ => ("Id", "Id")
                });

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row => (BigMap)ReadBigMap(row, micheline));
        }

        public async Task<object[][]> Get(
            AccountParameter contract,
            StringParameter path,
            BigMapTagsParameter tags,
            bool? active,
            Int32Parameter lastLevel,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string[] fields,
            MichelineFormat micheline)
        {
            var columns = new HashSet<string>(fields.Length);
            foreach (var field in fields)
            {
                switch (field)
                {
                    case "ptr": columns.Add(@"""Ptr"""); break;
                    case "contract": columns.Add(@"""ContractId"""); break;
                    case "path": columns.Add(@"""StoragePath"""); break;
                    case "active": columns.Add(@"""Active"""); break;
                    case "firstLevel": columns.Add(@"""FirstLevel"""); break;
                    case "lastLevel": columns.Add(@"""LastLevel"""); break;
                    case "totalKeys": columns.Add(@"""TotalKeys"""); break;
                    case "activeKeys": columns.Add(@"""ActiveKeys"""); break;
                    case "updates": columns.Add(@"""Updates"""); break;
                    case "keyType": columns.Add(@"""KeyType"""); break;
                    case "valueType": columns.Add(@"""ValueType"""); break;
                    case "tags": columns.Add(@"""Tags"""); break;
                }
            }

            if (columns.Count == 0)
                return Array.Empty<object[]>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""BigMaps""")
                .Filter("ContractId", contract)
                .Filter("StoragePath", path)
                .Filter("Tags", tags)
                .Filter("Active", active)
                .Filter("LastLevel", lastLevel)
                .Take(sort, offset, limit, x => x switch
                {
                    "ptr" => ("Ptr", "Ptr"),
                    "firstLevel" => ("Id", "FirstLevel"),
                    "lastLevel" => ("LastLevel", "LastLevel"),
                    "totalKeys" => ("TotalKeys", "TotalKeys"),
                    "activeKeys" => ("ActiveKeys", "ActiveKeys"),
                    "updates" => ("Updates", "Updates"),
                    _ => ("Id", "Id")
                });

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            var result = new object[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object[fields.Length];

            for (int i = 0, j = 0; i < fields.Length; j = 0, i++)
            {
                switch (fields[i])
                {
                    case "ptr":
                        foreach (var row in rows)
                            result[j++][i] = row.Ptr;
                        break;
                    case "contract":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.ContractId);
                        break;
                    case "path":
                        foreach (var row in rows)
                            result[j++][i] = row.StoragePath;
                        break;
                    case "active":
                        foreach (var row in rows)
                            result[j++][i] = row.Active;
                        break;
                    case "firstLevel":
                        foreach (var row in rows)
                            result[j++][i] = row.FirstLevel;
                        break;
                    case "lastLevel":
                        foreach (var row in rows)
                            result[j++][i] = row.LastLevel;
                        break;
                    case "totalKeys":
                        foreach (var row in rows)
                            result[j++][i] = row.TotalKeys;
                        break;
                    case "activeKeys":
                        foreach (var row in rows)
                            result[j++][i] = row.ActiveKeys;
                        break;
                    case "updates":
                        foreach (var row in rows)
                            result[j++][i] = row.Updates;
                        break;
                    case "keyType":
                        foreach (var row in rows)
                            result[j++][i] = (int)micheline < 2
                                ? (RawJson)Schema.Create(Micheline.FromBytes(row.KeyType) as MichelinePrim).Humanize()
                                : (RawJson)Micheline.ToJson(row.KeyType);
                        break;
                    case "valueType":
                        foreach (var row in rows)
                            result[j++][i] = (int)micheline < 2
                                ? (RawJson)Schema.Create(Micheline.FromBytes(row.ValueType) as MichelinePrim).Humanize()
                                : (RawJson)Micheline.ToJson(row.ValueType);
                        break;
                    case "tags":
                        foreach (var row in rows)
                            result[j++][i] = BigMapTags.ToList((Data.Models.BigMapTag)row.Tags);
                        break;
                }
            }

            return result;
        }

        public async Task<object[]> Get(
            AccountParameter contract,
            StringParameter path,
            BigMapTagsParameter tags,
            bool? active,
            Int32Parameter lastLevel,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string field,
            MichelineFormat micheline)
        {
            var columns = new HashSet<string>(1);
            switch (field)
            {
                case "ptr": columns.Add(@"""Ptr"""); break;
                case "contract": columns.Add(@"""ContractId"""); break;
                case "path": columns.Add(@"""StoragePath"""); break;
                case "active": columns.Add(@"""Active"""); break;
                case "firstLevel": columns.Add(@"""FirstLevel"""); break;
                case "lastLevel": columns.Add(@"""LastLevel"""); break;
                case "totalKeys": columns.Add(@"""TotalKeys"""); break;
                case "activeKeys": columns.Add(@"""ActiveKeys"""); break;
                case "updates": columns.Add(@"""Updates"""); break;
                case "keyType": columns.Add(@"""KeyType"""); break;
                case "valueType": columns.Add(@"""ValueType"""); break;
                case "tags": columns.Add(@"""Tags"""); break;
            }

            if (columns.Count == 0)
                return Array.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""BigMaps""")
                .Filter("ContractId", contract)
                .Filter("StoragePath", path)
                .Filter("Tags", tags)
                .Filter("Active", active)
                .Filter("LastLevel", lastLevel)
                .Take(sort, offset, limit, x => x switch
                {
                    "ptr" => ("Ptr", "Ptr"),
                    "firstLevel" => ("Id", "FirstLevel"),
                    "lastLevel" => ("LastLevel", "LastLevel"),
                    "totalKeys" => ("TotalKeys", "TotalKeys"),
                    "activeKeys" => ("ActiveKeys", "ActiveKeys"),
                    "updates" => ("Updates", "Updates"),
                    _ => ("Id", "Id")
                });

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            //TODO: optimize memory allocation
            var result = new object[rows.Count()];
            var j = 0;

            switch (field)
            {
                case "ptr":
                    foreach (var row in rows)
                        result[j++] = row.Ptr;
                    break;
                case "contract":
                    foreach (var row in rows)
                        result[j++] = Accounts.GetAlias(row.ContractId);
                    break;
                case "path":
                    foreach (var row in rows)
                        result[j++] = row.StoragePath;
                    break;
                case "active":
                    foreach (var row in rows)
                        result[j++] = row.Active;
                    break;
                case "firstLevel":
                    foreach (var row in rows)
                        result[j++] = row.FirstLevel;
                    break;
                case "lastLevel":
                    foreach (var row in rows)
                        result[j++] = row.LastLevel;
                    break;
                case "totalKeys":
                    foreach (var row in rows)
                        result[j++] = row.TotalKeys;
                    break;
                case "activeKeys":
                    foreach (var row in rows)
                        result[j++] = row.ActiveKeys;
                    break;
                case "updates":
                    foreach (var row in rows)
                        result[j++] = row.Updates;
                    break;
                case "keyType":
                    foreach (var row in rows)
                        result[j++] = (int)micheline < 2
                            ? (RawJson)Schema.Create(Micheline.FromBytes(row.KeyType) as MichelinePrim).Humanize()
                            : (RawJson)Micheline.ToJson(row.KeyType);
                    break;
                case "valueType":
                    foreach (var row in rows)
                        result[j++] = (int)micheline < 2
                            ? (RawJson)Schema.Create(Micheline.FromBytes(row.ValueType) as MichelinePrim).Humanize()
                            : (RawJson)Micheline.ToJson(row.ValueType);
                    break;
                case "tags":
                    foreach (var row in rows)
                        result[j++] = BigMapTags.ToList((Data.Models.BigMapTag)row.Tags);
                    break;
            }

            return result;
        }
        #endregion

        #region bigmap keys
        public async Task<BigMapKey> GetKey(
            int ptr,
            string key,
            MichelineFormat micheline)
        {
            var sql = @"
                SELECT  *
                FROM    ""BigMapKeys""
                WHERE   ""BigMapPtr"" = @ptr
                AND     ""JsonKey"" = @key::jsonb
                LIMIT   1";

            using var db = GetConnection();
            var row = await db.QueryFirstOrDefaultAsync(sql, new { ptr, key });
            if (row == null) return null;

            return ReadBigMapKey(row, micheline);
        }

        public async Task<BigMapKey> GetKeyByHash(
            int ptr,
            string hash,
            MichelineFormat micheline)
        {
            var sql = @"
                SELECT  *
                FROM    ""BigMapKeys""
                WHERE   ""BigMapPtr"" = @ptr
                AND     ""KeyHash"" = @hash::character(54)
                LIMIT   1";

            using var db = GetConnection();
            var row = await db.QueryFirstOrDefaultAsync(sql, new { ptr, hash });
            if (row == null) return null;

            return ReadBigMapKey(row, micheline);
        }

        public async Task<IEnumerable<BigMapKey>> GetKeys(
            int ptr,
            bool? active,
            JsonParameter key,
            JsonParameter value,
            Int32Parameter lastLevel,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            MichelineFormat micheline)
        {
            var sql = new SqlBuilder(@"SELECT * FROM ""BigMapKeys""")
                .Filter("BigMapPtr", ptr)
                .Filter("Active", active)
                .Filter("JsonKey", key)
                .Filter("JsonValue", value)
                .Filter("LastLevel", lastLevel)
                .Take(sort, offset, limit, x => x switch
                {
                    "firstLevel" => ("Id", "FirstLevel"),
                    "lastLevel" => ("LastLevel", "LastLevel"),
                    "updates" => ("Updates", "Updates"),
                    _ => ("Id", "Id")
                });

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row => (BigMapKey)ReadBigMapKey(row, micheline));
        }

        public async Task<object[][]> GetKeys(
            int ptr,
            bool? active,
            JsonParameter key,
            JsonParameter value,
            Int32Parameter lastLevel,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string[] fields,
            MichelineFormat micheline)
        {
            var columns = new HashSet<string>(fields.Length);
            foreach (var field in fields)
            {
                switch (field)
                {
                    case "active": columns.Add(@"""Active"""); break;
                    case "firstLevel": columns.Add(@"""FirstLevel"""); break;
                    case "lastLevel": columns.Add(@"""LastLevel"""); break;
                    case "updates": columns.Add(@"""Updates"""); break;
                    case "hash": columns.Add(@"""KeyHash"""); break;
                    case "key":
                        columns.Add((int)micheline < 2 ? @"""JsonKey""" : @"""RawKey""");
                        break;
                    case "value":
                        columns.Add((int)micheline < 2 ? @"""JsonValue""" : @"""RawValue""");
                        break;
                }
            }

            if (columns.Count == 0)
                return Array.Empty<object[]>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""BigMapKeys""")
                .Filter("BigMapPtr", ptr)
                .Filter("Active", active)
                .Filter("JsonKey", key)
                .Filter("JsonValue", value)
                .Filter("LastLevel", lastLevel)
                .Take(sort, offset, limit, x => x switch
                {
                    "firstLevel" => ("Id", "FirstLevel"),
                    "lastLevel" => ("LastLevel", "LastLevel"),
                    "updates" => ("Updates", "Updates"),
                    _ => ("Id", "Id")
                });

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            var result = new object[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object[fields.Length];

            for (int i = 0, j = 0; i < fields.Length; j = 0, i++)
            {
                switch (fields[i])
                {
                    case "active":
                        foreach (var row in rows)
                            result[j++][i] = row.Active;
                        break;
                    case "firstLevel":
                        foreach (var row in rows)
                            result[j++][i] = row.FirstLevel;
                        break;
                    case "lastLevel":
                        foreach (var row in rows)
                            result[j++][i] = row.LastLevel;
                        break;
                    case "updates":
                        foreach (var row in rows)
                            result[j++][i] = row.Updates;
                        break;
                    case "hash":
                        foreach (var row in rows)
                            result[j++][i] = row.KeyHash;
                        break;
                    case "key":
                        foreach (var row in rows)
                            result[j++][i] = FormatKey(row, micheline);
                        break;
                    case "value":
                        foreach (var row in rows)
                            result[j++][i] = FormatValue(row, micheline);
                        break;
                }
            }

            return result;
        }

        public async Task<object[]> GetKeys(
            int ptr,
            bool? active,
            JsonParameter key,
            JsonParameter value,
            Int32Parameter lastLevel,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string field,
            MichelineFormat micheline)
        {
            var columns = new HashSet<string>(1);
            switch (field)
            {
                case "active": columns.Add(@"""Active"""); break;
                case "firstLevel": columns.Add(@"""FirstLevel"""); break;
                case "lastLevel": columns.Add(@"""LastLevel"""); break;
                case "updates": columns.Add(@"""Updates"""); break;
                case "hash": columns.Add(@"""KeyHash"""); break;
                case "key":
                    columns.Add((int)micheline < 2 ? @"""JsonKey""" : @"""RawKey""");
                    break;
                case "value":
                    columns.Add((int)micheline < 2 ? @"""JsonValue""" : @"""RawValue""");
                    break;
            }

            if (columns.Count == 0)
                return Array.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""BigMapKeys""")
                .Filter("BigMapPtr", ptr)
                .Filter("Active", active)
                .Filter("JsonKey", key)
                .Filter("JsonValue", value)
                .Filter("LastLevel", lastLevel)
                .Take(sort, offset, limit, x => x switch
                {
                    "firstLevel" => ("Id", "FirstLevel"),
                    "lastLevel" => ("LastLevel", "LastLevel"),
                    "updates" => ("Updates", "Updates"),
                    _ => ("Id", "Id")
                });

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            //TODO: optimize memory allocation
            var result = new object[rows.Count()];
            var j = 0;

            switch (field)
            {
                case "active":
                    foreach (var row in rows)
                        result[j++] = row.Active;
                    break;
                case "firstLevel":
                    foreach (var row in rows)
                        result[j++] = row.FirstLevel;
                    break;
                case "lastLevel":
                    foreach (var row in rows)
                        result[j++] = row.LastLevel;
                    break;
                case "updates":
                    foreach (var row in rows)
                        result[j++] = row.Updates;
                    break;
                case "hash":
                    foreach (var row in rows)
                        result[j++] = row.KeyHash;
                    break;
                case "key":
                    foreach (var row in rows)
                        result[j++] = FormatKey(row, micheline);
                    break;
                case "value":
                    foreach (var row in rows)
                        result[j++] = FormatValue(row, micheline);
                    break;
            }

            return result;
        }
        #endregion

        #region historical keys
        async Task<BigMapKeyHistorical> GetHistoricalKey(
            BigMapKey key,
            int level,
            MichelineFormat micheline)
        {
            if (key == null || level < key.FirstLevel)
                return null;

            if (level > key.LastLevel)
                return new BigMapKeyHistorical
                {
                    Id = key.Id,
                    Hash = key.Hash,
                    Key = key.Key,
                    Value = key.Value,
                    Active = key.Active
                };

            var valCol = (int)micheline < 2 ? "JsonValue" : "RawValue";

            var sql = $@"
                SELECT   ""Action"", ""{valCol}""
                FROM     ""BigMapUpdates""
                WHERE    ""BigMapKeyId"" = {key.Id}
                AND      ""Level"" <= {level}
                ORDER BY ""Level"" DESC, ""Id"" DESC
                LIMIT    1";

            using var db = GetConnection();
            var row = await db.QueryFirstOrDefaultAsync(sql);
            if (row == null) return null;

            return new BigMapKeyHistorical
            {
                Id = key.Id,
                Hash = key.Hash,
                Key = key.Key,
                Value = FormatValue(row, micheline),
                Active = row.Action != (int)Data.Models.BigMapAction.RemoveKey
            };
        }

        public async Task<BigMapKeyHistorical> GetHistoricalKey(
            int ptr,
            int level,
            string key,
            MichelineFormat micheline)
        {
            return await GetHistoricalKey(await GetKey(ptr, key, micheline), level, micheline);
        }

        public async Task<BigMapKeyHistorical> GetHistoricalKeyByHash(
            int ptr,
            int level,
            string hash,
            MichelineFormat micheline)
        {
            return await GetHistoricalKey(await GetKeyByHash(ptr, hash, micheline), level, micheline);
        }

        public async Task<IEnumerable<BigMapKeyHistorical>> GetHistoricalKeys(
            int ptr,
            int level,
            bool? active,
            JsonParameter key,
            JsonParameter value,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            MichelineFormat micheline)
        {
            var (keyCol, valCol) = (int)micheline < 2
                ? ("JsonKey", "JsonValue")
                : ("RawKey", "RawValue");

            var subQuery = $@"
                SELECT DISTINCT ON (""BigMapKeyId"")     
                            k.""Id"", k.""KeyHash"", k.""{keyCol}"",
                            (u.""Action"" != {(int)Data.Models.BigMapAction.RemoveKey}) as ""Active"", u.""{valCol}""
                FROM        ""BigMapUpdates"" as u
                INNER JOIN  ""BigMapKeys"" as k
                        ON  k.""Id"" = u.""BigMapKeyId""
                WHERE       u.""BigMapPtr"" = {ptr}
                AND         u.""Level"" <= {level}
                ORDER BY    ""BigMapKeyId"", u.""Level"" DESC, u.""Id"" DESC";

            var sql = new SqlBuilder($"SELECT * from ({subQuery}) as updates")
                .Filter("Active", active)
                .Filter("JsonKey", key)
                .Filter("JsonValue", value)
                .Take(sort, offset, limit, x => ("Id", "Id"));

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row => (BigMapKeyHistorical)ReadBigMapKeyShort(row, micheline));
        }

        public async Task<object[][]> GetHistoricalKeys(
            int ptr,
            int level,
            bool? active,
            JsonParameter key,
            JsonParameter value,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string[] fields,
            MichelineFormat micheline)
        {
            var (keyCol, valCol) = (int)micheline < 2
                ? ("JsonKey", "JsonValue")
                : ("RawKey", "RawValue");

            var columns = new HashSet<string>(fields.Length);
            foreach (var field in fields)
            {
                switch (field)
                {
                    case "id": columns.Add(@"""Id"""); break;
                    case "active": columns.Add(@"""Active"""); break;
                    case "hash": columns.Add(@"""KeyHash"""); break;
                    case "key": columns.Add($@"""{keyCol}"""); break;
                    case "value": columns.Add($@"""{valCol}"""); break;
                }
            }

            if (columns.Count == 0)
                return Array.Empty<object[]>();

            var subQuery = $@"
                SELECT DISTINCT ON (""BigMapKeyId"")
                            k.""Id"", k.""KeyHash"", k.""{keyCol}"",
                            (u.""Action"" != {(int)Data.Models.BigMapAction.RemoveKey}) as ""Active"", u.""{valCol}""
                FROM        ""BigMapUpdates"" as u
                INNER JOIN  ""BigMapKeys"" as k
                        ON  k.""Id"" = u.""BigMapKeyId""
                WHERE       u.""BigMapPtr"" = {ptr}
                AND         u.""Level"" <= {level}
                ORDER BY    ""BigMapKeyId"", u.""Level"" DESC, u.""Id"" DESC";

            var sql = new SqlBuilder($"SELECT {string.Join(',', columns)} from ({subQuery}) as updates")
                .Filter("Active", active)
                .Filter("JsonKey", key)
                .Filter("JsonValue", value)
                .Take(sort, offset, limit, x => ("Id", "Id"));

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
                    case "active":
                        foreach (var row in rows)
                            result[j++][i] = row.Active;
                        break;
                    case "hash":
                        foreach (var row in rows)
                            result[j++][i] = row.KeyHash;
                        break;
                    case "key":
                        foreach (var row in rows)
                            result[j++][i] = FormatKey(row, micheline);
                        break;
                    case "value":
                        foreach (var row in rows)
                            result[j++][i] = FormatValue(row, micheline);
                        break;
                }
            }

            return result;
        }

        public async Task<object[]> GetHistoricalKeys(
            int ptr,
            int level,
            bool? active,
            JsonParameter key,
            JsonParameter value,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string field,
            MichelineFormat micheline)
        {
            var (keyCol, valCol) = (int)micheline < 2
                ? ("JsonKey", "JsonValue")
                : ("RawKey", "RawValue");

            var columns = new HashSet<string>(1);
            switch (field)
            {
                case "id": columns.Add(@"""Id"""); break;
                case "active": columns.Add(@"""Active"""); break;
                case "hash": columns.Add(@"""KeyHash"""); break;
                case "key": columns.Add($@"""{keyCol}"""); break;
                case "value": columns.Add($@"""{valCol}"""); break;
            }

            if (columns.Count == 0)
                return Array.Empty<object>();

            var subQuery = $@"
                SELECT DISTINCT ON (""BigMapKeyId"")
                            k.""Id"", k.""KeyHash"", k.""{keyCol}"",
                            (u.""Action"" != {(int)Data.Models.BigMapAction.RemoveKey}) as ""Active"", u.""{valCol}""
                FROM        ""BigMapUpdates"" as u
                INNER JOIN  ""BigMapKeys"" as k
                        ON  k.""Id"" = u.""BigMapKeyId""
                WHERE       u.""BigMapPtr"" = {ptr}
                AND         u.""Level"" <= {level}
                ORDER BY    ""BigMapKeyId"", u.""Level"" DESC, u.""Id"" DESC";

            var sql = new SqlBuilder($"SELECT {string.Join(',', columns)} from ({subQuery}) as updates")
                .Filter("Active", active)
                .Filter("JsonKey", key)
                .Filter("JsonValue", value)
                .Take(sort, offset, limit, x => ("Id", "Id"));

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
                case "active":
                    foreach (var row in rows)
                        result[j++] = row.Active;
                    break;
                case "hash":
                    foreach (var row in rows)
                        result[j++] = row.KeyHash;
                    break;
                case "key":
                    foreach (var row in rows)
                        result[j++] = FormatKey(row, micheline);
                    break;
                case "value":
                    foreach (var row in rows)
                        result[j++] = FormatValue(row, micheline);
                    break;
            }

            return result;
        }
        #endregion

        #region bigmap key updates
        public async Task<IEnumerable<BigMapKeyUpdate>> GetKeyUpdates(
            int ptr,
            string key,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            MichelineFormat micheline)
        {
            using var db = GetConnection();
            var keyRow = await db.QueryFirstOrDefaultAsync(@"
                SELECT  ""Id""
                FROM    ""BigMapKeys""
                WHERE   ""BigMapPtr"" = @ptr
                AND     ""JsonKey"" = @key::jsonb
                LIMIT   1",
                new { ptr, key });

            if (keyRow == null) return Enumerable.Empty<BigMapKeyUpdate>();

            var sql = new SqlBuilder(@"SELECT * FROM ""BigMapUpdates""")
                .Filter("BigMapKeyId", (int)keyRow.Id)
                .Take(sort, offset, limit, x => ("Id", "Id"));

            var rows = await db.QueryAsync(sql.Query, sql.Params);
            return rows.Select(row => (BigMapKeyUpdate)ReadBigMapUpdate(row, micheline));
        }

        public async Task<IEnumerable<BigMapKeyUpdate>> GetKeyByHashUpdates(
            int ptr,
            string hash,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            MichelineFormat micheline)
        {
            using var db = GetConnection();
            var keyRow = await db.QueryFirstOrDefaultAsync(@"
                SELECT  ""Id""
                FROM    ""BigMapKeys""
                WHERE   ""BigMapPtr"" = @ptr
                AND     ""KeyHash"" = @hash::character(54)
                LIMIT   1",
                new { ptr, hash });

            if (keyRow == null) return Enumerable.Empty<BigMapKeyUpdate>();

            var sql = new SqlBuilder(@"SELECT * FROM ""BigMapUpdates""")
                .Filter("BigMapKeyId", (int)keyRow.Id)
                .Take(sort, offset, limit, x => ("Id", "Id"));

            var rows = await db.QueryAsync(sql.Query, sql.Params);
            return rows.Select(row => (BigMapKeyUpdate)ReadBigMapUpdate(row, micheline));
        }
        #endregion

        #region bigmap updates
        public async Task<IEnumerable<BigMapUpdate>> GetUpdates(
            Int32Parameter ptr,
            BigMapActionParameter action,
            JsonParameter value,
            Int32Parameter level,
            TimestampParameter timestamp,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            MichelineFormat micheline)
        {
            var fCol = (int)micheline < 2 ? "Json" : "Raw";

            var sql = new SqlBuilder($@"SELECT ""Id"", ""BigMapPtr"", ""Action"", ""Level"", ""BigMapKeyId"", ""{fCol}Value"" FROM ""BigMapUpdates""")
                .Filter("BigMapPtr", ptr)
                .Filter("Action", action)
                .Filter("JsonValue", value)
                .Filter("Level", level)
                .Filter("Level", timestamp)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Id", "Level"),
                    _ => ("Id", "Id")
                });

            using var db = GetConnection();
            var updateRows = await db.QueryAsync(sql.Query, sql.Params);
            if (!updateRows.Any())
                return Enumerable.Empty<BigMapUpdate>();

            #region fetch keys
            var keyIds = updateRows
                .Where(x => x.BigMapKeyId != null)
                .Select(x => (int)x.BigMapKeyId)
                .Distinct()
                .ToList();

            var keyRows = keyIds.Any()
                ? (await db.QueryAsync($@"
                    SELECT ""Id"", ""KeyHash"", ""{fCol}Key"" FROM ""BigMapKeys""
                    WHERE ""Id"" = ANY(@keyIds)",
                    new { keyIds })).ToDictionary(x => (int)x.Id)
                : null;
            #endregion

            #region fetch bigmaps
            var bigmapPtrs = updateRows
                .Select(x => (int)x.BigMapPtr)
                .Distinct()
                .ToList();

            var bigmapRows = (await db.QueryAsync($@"
                SELECT ""Ptr"", ""ContractId"", ""StoragePath"", ""Tags"" FROM ""BigMaps""
                WHERE ""Ptr"" = ANY(@bigmapPtrs)",
                new { bigmapPtrs })).ToDictionary(x => (int)x.Ptr);
            #endregion

            return updateRows.Select(row =>
            {
                var bigmap = bigmapRows[(int)row.BigMapPtr];
                var key = row.BigMapKeyId == null ? null : keyRows[(int)row.BigMapKeyId];

                return new BigMapUpdate
                {
                    Id = row.Id,
                    Action = BigMapActions.ToString(row.Action),
                    Bigmap = row.BigMapPtr,
                    Level = row.Level,
                    Timestamp = Times[row.Level],
                    Contract = Accounts.GetAlias(bigmap.ContractId),
                    Path = bigmap.StoragePath,
                    _Tags = (Data.Models.BigMapTag)bigmap.Tags,
                    Content = key == null ? null : new BigMapKeyShort
                    {
                        Hash = key.KeyHash,
                        Key = FormatKey(key, micheline),
                        Value = FormatValue(row, micheline)
                    }

                };
            });
        }

        public async Task<IEnumerable<BigMapUpdate>> GetUpdates(
            Int32Parameter ptr,
            StringParameter path,
            AccountParameter contract,
            BigMapActionParameter action,
            JsonParameter value,
            BigMapTagsParameter tags,
            Int32Parameter level,
            TimestampParameter timestamp,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            MichelineFormat micheline)
        {
            var fCol = (int)micheline < 2 ? "Json" : "Raw";

            var sql = new SqlBuilder($@"
                SELECT  uu.""Id"", uu.""BigMapPtr"", uu.""Action"", uu.""Level"", uu.""BigMapKeyId"", uu.""{fCol}Value"",
                        bb.""ContractId"", bb.""StoragePath"", bb.""Tags""
                FROM ""BigMapUpdates"" as uu
                LEFT JOIN ""BigMaps"" as bb on bb.""Ptr"" = uu.""BigMapPtr""")
                .Filter("BigMapPtr", ptr)
                .Filter("StoragePath", path)
                .Filter("Action", action)
                .Filter("JsonValue", value)
                .Filter("Tags", tags)
                .Filter("ContractId", contract)
                .Filter("Level", level)
                .Filter("Level", timestamp)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Id", "Level"),
                    _ => ("Id", "Id")
                }, "uu");

            using var db = GetConnection();
            var updateRows = await db.QueryAsync(sql.Query, sql.Params);
            if (!updateRows.Any())
                return Enumerable.Empty<BigMapUpdate>();

            #region fetch keys
            var keyIds = updateRows
                .Where(x => x.BigMapKeyId != null)
                .Select(x => (int)x.BigMapKeyId)
                .Distinct()
                .ToList();

            var keyRows = keyIds.Any()
                ? (await db.QueryAsync($@"
                    SELECT ""Id"", ""KeyHash"", ""{fCol}Key"" FROM ""BigMapKeys""
                    WHERE ""Id"" = ANY(@keyIds)",
                    new { keyIds })).ToDictionary(x => (int)x.Id)
                : null;
            #endregion

            return updateRows.Select(row =>
            {
                var key = row.BigMapKeyId == null ? null : keyRows[(int)row.BigMapKeyId];

                return new BigMapUpdate
                {
                    Id = row.Id,
                    Action = BigMapActions.ToString(row.Action),
                    Bigmap = row.BigMapPtr,
                    Level = row.Level,
                    Timestamp = Times[row.Level],
                    Contract = Accounts.GetAlias(row.ContractId),
                    Path = row.StoragePath,
                    _Tags = (Data.Models.BigMapTag)row.Tags,
                    Content = key == null ? null : new BigMapKeyShort
                    {
                        Hash = key.KeyHash,
                        Key = FormatKey(key, micheline),
                        Value = FormatValue(row, micheline)
                    }

                };
            });
        }
        #endregion

        #region diffs
        public static Task<Dictionary<long, List<BigMapDiff>>> GetTransactionDiffs(IDbConnection db, List<long> ops, MichelineFormat format)
            => GetBigMapDiffs(db, ops, nameof(Data.Models.BigMapUpdate.TransactionId), format);

        public static Task<Dictionary<long, List<BigMapDiff>>> GetOriginationDiffs(IDbConnection db, List<long> ops, MichelineFormat format)
            => GetBigMapDiffs(db, ops, nameof(Data.Models.BigMapUpdate.OriginationId), format);

        public static Task<Dictionary<long, List<BigMapDiff>>> GetMigrationDiffs(IDbConnection db, List<long> ops, MichelineFormat format)
            => GetBigMapDiffs(db, ops, nameof(Data.Models.BigMapUpdate.MigrationId), format);

        static async Task<Dictionary<long, List<BigMapDiff>>> GetBigMapDiffs(IDbConnection db, List<long> ops, string opCol, MichelineFormat format)
        {
            if (ops.Count == 0) return null;

            var fCol = (int)format < 2 ? "Json" : "Raw";

            var rows = await db.QueryAsync($@"
                SELECT ""BigMapPtr"", ""Action"", ""BigMapKeyId"", ""{fCol}Value"", ""{opCol}"" as ""OpId""
                FROM ""BigMapUpdates""
                WHERE ""{opCol}"" = ANY(@ops)
                ORDER BY ""Id""",
                new { ops });

            if (!rows.Any()) return null;

            var ptrs = rows
                .Select(x => (int)x.BigMapPtr)
                .Distinct()
                .ToList();

            var bigmaps = (await db.QueryAsync($@"
                SELECT ""Ptr"", ""StoragePath""
                FROM ""BigMaps""
                WHERE ""Ptr"" = ANY(@ptrs)",
                new { ptrs }))
                .ToDictionary(x => (int)x.Ptr);

            var keyIds = rows
                .Where(x => x.BigMapKeyId != null)
                .Select(x => (int)x.BigMapKeyId)
                .Distinct()
                .ToList();

            var keys = keyIds.Count == 0 ? null : (await db.QueryAsync($@"
                SELECT ""Id"", ""KeyHash"", ""{fCol}Key""
                FROM ""BigMapKeys""
                WHERE ""Id"" = ANY(@keyIds)",
                new { keyIds }))
                .ToDictionary(x => (int)x.Id);

            var res = new Dictionary<long, List<BigMapDiff>>(rows.Count());
            foreach (var row in rows)
            {
                if (!res.TryGetValue((long)row.OpId, out var list))
                {
                    list = new List<BigMapDiff>();
                    res.Add((long)row.OpId, list);
                }
                list.Add(new BigMapDiff
                {
                    Bigmap = row.BigMapPtr,
                    Path = bigmaps[row.BigMapPtr].StoragePath,
                    Action = BigMapActions.ToString(row.Action),
                    Content = row.BigMapKeyId == null ? null : new BigMapKeyShort
                    {
                        Hash = keys[row.BigMapKeyId].KeyHash,
                        Key = FormatKey(keys[row.BigMapKeyId], format),
                        Value = FormatValue(row, format),
                    }
                });
            }
            return res;
        }
        #endregion

        BigMap ReadBigMap(dynamic row, MichelineFormat format)
        {
            return new BigMap
            {
                Ptr = row.Ptr,
                Contract = Accounts.GetAlias(row.ContractId),
                Path = row.StoragePath,
                Active = row.Active,
                FirstLevel = row.FirstLevel,
                LastLevel = row.LastLevel,
                TotalKeys = row.TotalKeys,
                ActiveKeys = row.ActiveKeys,
                Updates = row.Updates,
                KeyType = (int)format < 2
                    ? (RawJson)Schema.Create(Micheline.FromBytes(row.KeyType) as MichelinePrim).Humanize()
                    : (RawJson)Micheline.ToJson(row.KeyType),
                ValueType = (int)format < 2
                    ? (RawJson)Schema.Create(Micheline.FromBytes(row.ValueType) as MichelinePrim).Humanize()
                    : (RawJson)Micheline.ToJson(row.ValueType),
                _Tags = (Data.Models.BigMapTag)row.Tags
            };
        }

        BigMapKey ReadBigMapKey(dynamic row, MichelineFormat format)
        {
            return new BigMapKey
            {
                Id = row.Id,
                Active = row.Active,
                FirstLevel = row.FirstLevel,
                LastLevel = row.LastLevel,
                Updates = row.Updates,
                Hash = row.KeyHash,
                Key = FormatKey(row, format),
                Value = FormatValue(row, format)
            };
        }

        BigMapKeyHistorical ReadBigMapKeyShort(dynamic row, MichelineFormat format)
        {
            return new BigMapKeyHistorical
            {
                Id = row.Id,
                Active = row.Active,
                Hash = row.KeyHash,
                Key = FormatKey(row, format),
                Value = FormatValue(row, format)
            };
        }

        BigMapKeyUpdate ReadBigMapUpdate(dynamic row, MichelineFormat format)
        {
            return new BigMapKeyUpdate
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = Times[row.Level],
                Action = BigMapActions.ToString(row.Action),
                Value = FormatValue(row, format)
            };
        }

        static object FormatKey(dynamic row, MichelineFormat format) => format switch
        {
            MichelineFormat.Json => (RawJson)row.JsonKey,
            MichelineFormat.JsonString => row.JsonKey,
            MichelineFormat.Raw => (RawJson)Micheline.ToJson(row.RawKey),
            MichelineFormat.RawString => Micheline.ToJson(row.RawKey),
            _ => null
        };

        static object FormatValue(dynamic row, MichelineFormat format) => format switch
        {
            MichelineFormat.Json => (RawJson)row.JsonValue,
            MichelineFormat.JsonString => row.JsonValue,
            MichelineFormat.Raw => (RawJson)Micheline.ToJson(row.RawValue),
            MichelineFormat.RawString => Micheline.ToJson(row.RawValue),
            _ => null
        };
    }
}
