using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Dapper;
using Netezos.Encoding;

using Tzkt.Api.Models;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Repositories
{
    public class ConstantsRepository : DbConnection
    {
        readonly AccountsCache Accounts;
        readonly TimeCache Time;

        public ConstantsRepository(AccountsCache accounts, TimeCache time, IConfiguration config) : base(config)
        {
            Accounts = accounts;
            Time = time;
        }

        public async Task<int> GetCount()
        {
            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(@"SELECT COUNT(*) FROM ""RegisterConstantOps"" WHERE ""Address"" IS NOT NULL");
        }

        public async Task<IEnumerable<Constant>> Get(
            ExpressionParameter address,
            Int32Parameter creationLevel,
            TimestampParameter creationTime,
            AccountParameter creator,
            Int32Parameter refs,
            Int32Parameter size,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            int format)
        {
            var sql = new SqlBuilder(@"SELECT * FROM ""RegisterConstantOps""")
                .Filter(@"""Address"" IS NOT NULL")
                .Filter("Address", address)
                .Filter("Level", creationLevel)
                .Filter("Level", creationTime)
                .Filter("SenderId", creator)
                .Filter("Refs", refs)
                .Filter("StorageUsed", size)
                .Take(sort, offset, limit, x => x switch
                {
                    "creationLevel" => ("Level", "Level"),
                    "size" => ("StorageUsed", "StorageUsed"),
                    "refs" => ("Refs", "Refs"),
                    _ => ("Id", "Id")
                });

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row => new Constant
            {
                Address = row.Address,
                CreationLevel = row.Level,
                CreationTime = row.Timestamp,
                Creator = Accounts.GetAlias(row.SenderId),
                Refs = row.Refs,
                Size = row.StorageUsed,
                Value = FormatConstantValue(row.Value, format),
                Extras = row.Extras
            });
        }

        public async Task<object[][]> Get(
            ExpressionParameter address,
            Int32Parameter creationLevel,
            TimestampParameter creationTime,
            AccountParameter creator,
            Int32Parameter refs,
            Int32Parameter size,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string[] fields,
            int format)
        {
            var columns = new HashSet<string>(fields.Length);
            foreach (var field in fields)
            {
                switch (field)
                {
                    case "address": columns.Add(@"""Address"""); break;
                    case "creationLevel": columns.Add(@"""Level"""); break;
                    case "creationTime": columns.Add(@"""Timestamp"""); break;
                    case "creator": columns.Add(@"""SenderId"""); break;
                    case "refs": columns.Add(@"""Refs"""); break;
                    case "size": columns.Add(@"""StorageUsed"""); break;
                    case "value": columns.Add(@"""Value"""); break;
                    case "extras": columns.Add(@"""Extras"""); break;
                }
            }

            if (columns.Count == 0)
                return Array.Empty<object[]>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""RegisterConstantOps""")
                .Filter(@"""Address"" IS NOT NULL")
                .Filter("Address", address)
                .Filter("Level", creationLevel)
                .Filter("Level", creationTime)
                .Filter("SenderId", creator)
                .Filter("Refs", refs)
                .Filter("StorageUsed", size)
                .Take(sort, offset, limit, x => x switch
                {
                    "creationLevel" => ("Level", "Level"),
                    "size" => ("StorageUsed", "StorageUsed"),
                    "refs" => ("Refs", "Refs"),
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
                    case "address":
                        foreach (var row in rows)
                            result[j++][i] = row.Address;
                        break;
                    case "creationLevel":
                        foreach (var row in rows)
                            result[j++][i] = row.Level;
                        break;
                    case "creationTime":
                        foreach (var row in rows)
                            result[j++][i] = row.Timestamp;
                        break;
                    case "creator":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.SenderId);
                        break;
                    case "refs":
                        foreach (var row in rows)
                            result[j++][i] = row.Refs;
                        break;
                    case "size":
                        foreach (var row in rows)
                            result[j++][i] = row.StorageUsed;
                        break;
                    case "value":
                        foreach (var row in rows)
                            result[j++][i] = FormatConstantValue(row.Value, format);
                        break;
                    case "extras":
                        foreach (var row in rows)
                            result[j++][i] = (RawJson)row.Extras;
                        break;
                }
            }

            return result;
        }

        public async Task<object[]> Get(
            ExpressionParameter address,
            Int32Parameter creationLevel,
            TimestampParameter creationTime,
            AccountParameter creator,
            Int32Parameter refs,
            Int32Parameter size,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string field,
            int format)
        {
            var columns = new HashSet<string>(1);
            switch (field)
            {
                case "address": columns.Add(@"""Address"""); break;
                case "creationLevel": columns.Add(@"""Level"""); break;
                case "creationTime": columns.Add(@"""Timestamp"""); break;
                case "creator": columns.Add(@"""SenderId"""); break;
                case "refs": columns.Add(@"""Refs"""); break;
                case "size": columns.Add(@"""StorageUsed"""); break;
                case "value": columns.Add(@"""Value"""); break;
                case "extras": columns.Add(@"""Extras"""); break;
            }

            if (columns.Count == 0)
                return Array.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""RegisterConstantOps""")
                .Filter(@"""Address"" IS NOT NULL")
                .Filter("Address", address)
                .Filter("Level", creationLevel)
                .Filter("Level", creationTime)
                .Filter("SenderId", creator)
                .Filter("Refs", refs)
                .Filter("StorageUsed", size)
                .Take(sort, offset, limit, x => x switch
                {
                    "creationLevel" => ("Level", "Level"),
                    "size" => ("StorageUsed", "StorageUsed"),
                    "refs" => ("Refs", "Refs"),
                    _ => ("Id", "Id")
                });

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            var result = new object[rows.Count()];
            var j = 0;

            switch (field)
            {
                case "address":
                    foreach (var row in rows)
                        result[j++] = row.Address;
                    break;
                case "creationLevel":
                    foreach (var row in rows)
                        result[j++] = row.Level;
                    break;
                case "creationTime":
                    foreach (var row in rows)
                        result[j++] = row.Timestamp;
                    break;
                case "creator":
                    foreach (var row in rows)
                        result[j++] = Accounts.GetAlias(row.SenderId);
                    break;
                case "refs":
                    foreach (var row in rows)
                        result[j++] = row.Refs;
                    break;
                case "size":
                    foreach (var row in rows)
                        result[j++] = row.StorageUsed;
                    break;
                case "value":
                    foreach (var row in rows)
                        result[j++] = FormatConstantValue(row.Value, format);
                    break;
                case "extras":
                    foreach (var row in rows)
                        result[j++] = (RawJson)row.Extras;
                    break;
            }

            return result;
        }

        static object FormatConstantValue(byte[] value, int format) => format switch
        {
            0 => Micheline.FromBytes(value),
            1 => Micheline.FromBytes(value).ToMichelson(),
            2 => value,
            _ => null
        };
    }
}
