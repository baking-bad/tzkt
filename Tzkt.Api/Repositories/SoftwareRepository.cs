﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Dapper;
using Tzkt.Api.Models;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Repositories
{
    public class SoftwareRepository : DbConnection
    {
        readonly TimeCache Time;

        public SoftwareRepository(TimeCache time, IConfiguration config) : base(config)
        {
            Time = time;
        }

        public async Task Update(string hash, string metadata)
        {
            using var db = GetConnection();
            var upd = $@"UPDATE ""Software"" SET ""Metadata"" = '{metadata}' WHERE ""ShortHash"" = '{hash}'";
            //TODO metadata to parameters
            var rows = await db.ExecuteAsync(upd);
        }

        public async Task<int> GetCount()
        {
            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(@"SELECT COUNT(*) FROM ""Software""");
        }

        public async Task<IEnumerable<Software>> Get(SortParameter sort, OffsetParameter offset, int limit)
        {
            var sql = new SqlBuilder(@"SELECT * FROM ""Software""")
                .Take(sort, offset, limit, x => x switch
                {
                    "firstLevel" => ("FirstLevel", "FirstLevel"),
                    "lastLevel" => ("LastLevel", "LastLevel"),
                    "blocksCount" => ("BlocksCount", "BlocksCount"),
                    _ => ("Id", "Id")
                });

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row => new Software
            {
                BlocksCount = row.BlocksCount,
                FirstLevel = row.FirstLevel,
                FirstTime = Time[row.FirstLevel],
                LastLevel = row.LastLevel,
                LastTime = Time[row.LastLevel],
                ShortHash = row.ShortHash,
                Metadata = row.Metadata == null ? null : new RawJson(row.Metadata)
            });
        }

        public async Task<object[][]> Get(SortParameter sort, OffsetParameter offset, int limit, string[] fields)
        {
            var columns = new HashSet<string>(fields.Length);
            foreach (var field in fields)
            {
                switch (field)
                {
                    case "blocksCount": columns.Add(@"""BlocksCount"""); break;
                    case "firstLevel": columns.Add(@"""FirstLevel"""); break;
                    case "firstTime": columns.Add(@"""FirstLevel"""); break;
                    case "lastLevel": columns.Add(@"""LastLevel"""); break;
                    case "lastTime": columns.Add(@"""LastLevel"""); break;
                    case "shortHash": columns.Add(@"""ShortHash"""); break;
                    case "metadata": columns.Add(@"""Metadata"""); break;
                }
            }

            if (columns.Count == 0)
                return Array.Empty<object[]>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""Software""")
                .Take(sort, offset, limit, x => x switch
                {
                    "firstLevel" => ("FirstLevel", "FirstLevel"),
                    "lastLevel" => ("LastLevel", "LastLevel"),
                    "blocksCount" => ("BlocksCount", "BlocksCount"),
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
                    case "blocksCount":
                        foreach (var row in rows)
                            result[j++][i] = row.BlocksCount;
                        break;
                    case "firstLevel":
                        foreach (var row in rows)
                            result[j++][i] = row.FirstLevel;
                        break;
                    case "firstTime":
                        foreach (var row in rows)
                            result[j++][i] = Time[row.FirstLevel];
                        break;
                    case "lastLevel":
                        foreach (var row in rows)
                            result[j++][i] = row.LastLevel;
                        break;
                    case "lastTime":
                        foreach (var row in rows)
                            result[j++][i] = Time[row.LastLevel];
                        break;
                    case "shortHash":
                        foreach (var row in rows)
                            result[j++][i] = row.ShortHash;
                        break;
                    case "metadata":
                        foreach (var row in rows)
                            result[j++][i] = row.Metadata == null ? null : new RawJson(row.Metadata);
                        break;
                }
            }

            return result;
        }

        public async Task<object[]> Get(SortParameter sort, OffsetParameter offset, int limit, string field)
        {
            var columns = new HashSet<string>(1);
            switch (field)
            {
                case "blocksCount": columns.Add(@"""BlocksCount"""); break;
                case "firstLevel": columns.Add(@"""FirstLevel"""); break;
                case "firstTime": columns.Add(@"""FirstLevel"""); break;
                case "lastLevel": columns.Add(@"""LastLevel"""); break;
                case "lastTime": columns.Add(@"""LastLevel"""); break;
                case "shortHash": columns.Add(@"""ShortHash"""); break;
                case "metadata": columns.Add(@"""Metadata"""); break;
            }

            if (columns.Count == 0)
                return Array.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""Software""")
                .Take(sort, offset, limit, x => x switch
                {
                    "firstLevel" => ("FirstLevel", "FirstLevel"),
                    "lastLevel" => ("LastLevel", "LastLevel"),
                    "blocksCount" => ("BlocksCount", "BlocksCount"),
                    _ => ("Id", "Id")
                });

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            var result = new object[rows.Count()];
            var j = 0;

            switch (field)
            {
                case "blocksCount":
                    foreach (var row in rows)
                        result[j++] = row.BlocksCount;
                    break;
                case "firstLevel":
                    foreach (var row in rows)
                        result[j++] = row.FirstLevel;
                    break;
                case "firstTime":
                    foreach (var row in rows)
                        result[j++] = Time[row.FirstLevel];
                    break;
                case "lastLevel":
                    foreach (var row in rows)
                        result[j++] = row.LastLevel;
                    break;
                case "lastTime":
                    foreach (var row in rows)
                        result[j++] = Time[row.LastLevel];
                    break;
                case "shortHash":
                    foreach (var row in rows)
                        result[j++] = row.ShortHash;
                    break;
                case "metadata":
                    foreach (var row in rows)
                        result[j++] = row.Metadata == null ? null : new RawJson(row.Metadata);
                    break;
            }

            return result;
        }
    }
}
