using System;
using System.Collections.Generic;
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

        public BigMapsRepository(AccountsCache accounts, IConfiguration config) : base(config)
        {
            Accounts = accounts;
        }

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

            return new BigMap
            {
                Ptr = row.Ptr,
                Contract = Accounts.GetAlias(row.ContractId),
                Path = ((string)row.StoragePath).Replace(".", "..").Replace(',', '.'),
                Active = row.Active,
                FirstLevel = row.FirstLevel,
                LastLevel = row.LastLevel,
                TotalKeys = row.TotalKeys,
                ActiveKeys = row.ActiveKeys,
                Updates = row.Updates,
                KeyType = (int)micheline < 2 
                    ? new JsonString(Schema.Create(Micheline.FromBytes(row.KeyType) as MichelinePrim).Humanize())
                    : Micheline.FromBytes(row.KeyType),
                ValueType = (int)micheline < 2
                    ? new JsonString(Schema.Create(Micheline.FromBytes(row.ValueType) as MichelinePrim).Humanize())
                    : Micheline.FromBytes(row.ValueType)
            };
        }

        public async Task<IEnumerable<BigMap>> Get(
            AccountParameter contract,
            BoolParameter active,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            MichelineFormat micheline)
        {
            var sql = new SqlBuilder(@"SELECT * FROM ""BigMaps""")
                .Filter("ContractId", contract)
                .Filter("Active", active)
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

            return rows.Select(row => new BigMap
            {
                Ptr = row.Ptr,
                Contract = Accounts.GetAlias(row.ContractId),
                Path = ((string)row.StoragePath).Replace(".", "..").Replace(',', '.'),
                Active = row.Active,
                FirstLevel = row.FirstLevel,
                LastLevel = row.LastLevel,
                TotalKeys = row.TotalKeys,
                ActiveKeys = row.ActiveKeys,
                Updates = row.Updates,
                KeyType = (int)micheline < 2
                    ? new JsonString(Schema.Create(Micheline.FromBytes(row.KeyType) as MichelinePrim).Humanize())
                    : Micheline.FromBytes(row.KeyType),
                ValueType = (int)micheline < 2
                    ? new JsonString(Schema.Create(Micheline.FromBytes(row.ValueType) as MichelinePrim).Humanize())
                    : Micheline.FromBytes(row.ValueType)
            });
        }

        public async Task<object[][]> Get(
            AccountParameter contract,
            BoolParameter active,
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
                }
            }

            if (columns.Count == 0)
                return Array.Empty<object[]>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""BigMaps""")
                .Filter("ContractId", contract)
                .Filter("Active", active)
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
                            result[j++][i] = ((string)row.StoragePath).Replace(".", "..").Replace(',', '.');
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
                                ? new JsonString(Schema.Create(Micheline.FromBytes(row.KeyType) as MichelinePrim).Humanize())
                                : Micheline.FromBytes(row.KeyType);
                        break;
                    case "valueType":
                        foreach (var row in rows)
                            result[j++][i] = (int)micheline < 2
                                ? new JsonString(Schema.Create(Micheline.FromBytes(row.ValueType) as MichelinePrim).Humanize())
                                : Micheline.FromBytes(row.ValueType);
                        break;
                }
            }

            return result;
        }

        public async Task<object[]> Get(
            AccountParameter contract,
            BoolParameter active,
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
            }

            if (columns.Count == 0)
                return Array.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""BigMaps""")
                .Filter("ContractId", contract)
                .Filter("Active", active)
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
                        result[j++] = ((string)row.StoragePath).Replace(".", "..").Replace(',', '.');
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
                            ? new JsonString(Schema.Create(Micheline.FromBytes(row.KeyType) as MichelinePrim).Humanize())
                            : Micheline.FromBytes(row.KeyType);
                    break;
                case "valueType":
                    foreach (var row in rows)
                        result[j++] = (int)micheline < 2
                            ? new JsonString(Schema.Create(Micheline.FromBytes(row.ValueType) as MichelinePrim).Humanize())
                            : Micheline.FromBytes(row.ValueType);
                    break;
            }

            return result;
        }
    }
}
