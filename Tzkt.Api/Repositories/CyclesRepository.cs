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
    public class CyclesRepository : DbConnection
    {
        readonly QuotesCache Quotes;
        readonly TimeCache Times;

        public CyclesRepository(QuotesCache quotes, TimeCache times, IConfiguration config) : base(config)
        {
            Quotes = quotes;
            Times = times;
        }

        public async Task<int> GetCount()
        {
            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(@"SELECT COUNT(*) FROM ""Cycles""");
        }

        public async Task<Cycle> Get(int index, Symbols quote)
        {
            var sql = @"
                SELECT  *
                FROM    ""Cycles""
                WHERE   ""Index"" = @index
                LIMIT   1";

            using var db = GetConnection();
            var row = await db.QueryFirstOrDefaultAsync(sql, new { index });
            if (row == null) return null;

            return new Cycle
            {
                Index = row.Index,
                FirstLevel = row.FirstLevel,
                StartTime = Times[row.FirstLevel],
                LastLevel = row.LastLevel,
                EndTime = Times[row.LastLevel],
                RandomSeed = Hex.Convert(row.Seed),
                SnapshotIndex = row.SnapshotIndex,
                SnapshotLevel = row.SnapshotLevel,
                TotalBakers = row.TotalBakers,
                TotalDelegated = row.TotalDelegated,
                TotalDelegators = row.TotalDelegators,
                TotalStaking = row.TotalStaking,
                SelectedBakers = row.SelectedBakers,
                SelectedStake = row.SelectedStake,
                Quote = Quotes.Get(quote, row.LastLevel)
            };
        }

        public async Task<IEnumerable<Cycle>> Get(
            Int32Parameter snapshotIndex,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            Symbols quote)
        {
            var sql = new SqlBuilder(@"SELECT * FROM ""Cycles""")
                .Filter("SnapshotIndex", snapshotIndex)
                .Take(sort ?? new SortParameter { Desc = "index" }, offset, limit, x => ("Index", "Index"));

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row => new Cycle
            {
                Index = row.Index,
                FirstLevel = row.FirstLevel,
                StartTime = Times[row.FirstLevel],
                LastLevel = row.LastLevel,
                EndTime = Times[row.LastLevel],
                RandomSeed = Hex.Convert(row.Seed),
                SnapshotIndex = row.SnapshotIndex,
                SnapshotLevel = row.SnapshotLevel,
                TotalBakers = row.TotalBakers,
                TotalDelegated = row.TotalDelegated,
                TotalDelegators = row.TotalDelegators,
                TotalStaking = row.TotalStaking,
                SelectedBakers = row.SelectedBakers,
                SelectedStake = row.SelectedStake,
                Quote = Quotes.Get(quote, row.LastLevel)
            });
        }

        public async Task<object[][]> Get(
            Int32Parameter snapshotIndex,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string[] fields,
            Symbols quote)
        {
            var columns = new HashSet<string>(fields.Length);
            foreach (var field in fields)
            {
                switch (field)
                {
                    case "index": columns.Add(@"""Index"""); break;
                    case "firstLevel": columns.Add(@"""FirstLevel"""); break;
                    case "startTime": columns.Add(@"""FirstLevel"""); break;
                    case "lastLevel": columns.Add(@"""LastLevel"""); break;
                    case "endTime": columns.Add(@"""LastLevel"""); break;
                    case "randomSeed": columns.Add(@"""Seed"""); break;
                    case "snapshotIndex": columns.Add(@"""SnapshotIndex"""); break;
                    case "snapshotLevel": columns.Add(@"""SnapshotLevel"""); break;
                    case "totalBakers": columns.Add(@"""TotalBakers"""); break;
                    case "totalDelegated": columns.Add(@"""TotalDelegated"""); break;
                    case "totalDelegators": columns.Add(@"""TotalDelegators"""); break;
                    case "totalStaking": columns.Add(@"""TotalStaking"""); break;
                    case "selectedBakers": columns.Add(@"""SelectedBakers"""); break;
                    case "selectedStake": columns.Add(@"""SelectedStake"""); break;
                    case "quote": columns.Add(@"""LastLevel"""); break;
                }
            }

            if (columns.Count == 0)
                return Array.Empty<object[]>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""Cycles""")
                .Filter("SnapshotIndex", snapshotIndex)
                .Take(sort ?? new SortParameter { Desc = "index" }, offset, limit, x => ("Index", "Index"));

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            var result = new object[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object[fields.Length];

            for (int i = 0, j = 0; i < fields.Length; j = 0, i++)
            {
                switch (fields[i])
                {
                    case "index":
                        foreach (var row in rows)
                            result[j++][i] = row.Index;
                        break;
                    case "firstLevel":
                        foreach (var row in rows)
                            result[j++][i] = row.FirstLevel;
                        break;
                    case "startTime":
                        foreach (var row in rows)
                            result[j++][i] = Times[row.FirstLevel];
                        break;
                    case "lastLevel":
                        foreach (var row in rows)
                            result[j++][i] = row.LastLevel;
                        break;
                    case "endTime":
                        foreach (var row in rows)
                            result[j++][i] = Times[row.LastLevel];
                        break;
                    case "randomSeed":
                        foreach (var row in rows)
                            result[j++][i] = Hex.Convert(row.Seed);
                        break;
                    case "snapshotIndex":
                        foreach (var row in rows)
                            result[j++][i] = row.SnapshotIndex;
                        break;
                    case "snapshotLevel":
                        foreach (var row in rows)
                            result[j++][i] = row.SnapshotLevel;
                        break;
                    case "totalBakers":
                        foreach (var row in rows)
                            result[j++][i] = row.TotalBakers;
                        break;
                    case "totalDelegated":
                        foreach (var row in rows)
                            result[j++][i] = row.TotalDelegated;
                        break;
                    case "totalDelegators":
                        foreach (var row in rows)
                            result[j++][i] = row.TotalDelegators;
                        break;
                    case "totalStaking":
                        foreach (var row in rows)
                            result[j++][i] = row.TotalStaking;
                        break;
                    case "selectedBakers":
                        foreach (var row in rows)
                            result[j++][i] = row.SelectedBakers;
                        break;
                    case "selectedStake":
                        foreach (var row in rows)
                            result[j++][i] = row.SelectedStake;
                        break;
                    case "quote":
                        foreach (var row in rows)
                            result[j++][i] = Quotes.Get(quote, row.LastLevel);
                        break;
                }
            }

            return result;
        }

        public async Task<object[]> Get(
            Int32Parameter snapshotIndex,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string field,
            Symbols quote)
        {
            var columns = new HashSet<string>(1);
            switch (field)
            {
                case "index": columns.Add(@"""Index"""); break;
                case "firstLevel": columns.Add(@"""FirstLevel"""); break;
                case "startTime": columns.Add(@"""FirstLevel"""); break;
                case "lastLevel": columns.Add(@"""LastLevel"""); break;
                case "endTime": columns.Add(@"""LastLevel"""); break;
                case "randomSeed": columns.Add(@"""Seed"""); break;
                case "snapshotIndex": columns.Add(@"""SnapshotIndex"""); break;
                case "snapshotLevel": columns.Add(@"""SnapshotLevel"""); break;
                case "totalBakers": columns.Add(@"""TotalBakers"""); break;
                case "totalDelegated": columns.Add(@"""TotalDelegated"""); break;
                case "totalDelegators": columns.Add(@"""TotalDelegators"""); break;
                case "totalStaking": columns.Add(@"""TotalStaking"""); break;
                case "selectedBakers": columns.Add(@"""SelectedBakers"""); break;
                case "selectedStake": columns.Add(@"""SelectedStake"""); break;
                case "quote": columns.Add(@"""LastLevel"""); break;
            }

            if (columns.Count == 0)
                return Array.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""Cycles""")
                .Filter("SnapshotIndex", snapshotIndex)
                .Take(sort ?? new SortParameter { Desc = "index" }, offset, limit, x => ("Index", "Index"));

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            //TODO: optimize memory allocation
            var result = new object[rows.Count()];
            var j = 0;

            switch (field)
            {
                case "index":
                    foreach (var row in rows)
                        result[j++] = row.Index;
                    break;
                case "firstLevel":
                    foreach (var row in rows)
                        result[j++] = row.FirstLevel;
                    break;
                case "startTime":
                    foreach (var row in rows)
                        result[j++] = Times[row.FirstLevel];
                    break;
                case "lastLevel":
                    foreach (var row in rows)
                        result[j++] = row.LastLevel;
                    break;
                case "endTime":
                    foreach (var row in rows)
                        result[j++] = Times[row.LastLevel];
                    break;
                case "randomSeed":
                    foreach (var row in rows)
                        result[j++] = Hex.Convert(row.Seed);
                    break;
                case "snapshotIndex":
                    foreach (var row in rows)
                        result[j++] = row.SnapshotIndex;
                    break;
                case "snapshotLevel":
                    foreach (var row in rows)
                        result[j++] = row.SnapshotLevel;
                    break;
                case "totalBakers":
                    foreach (var row in rows)
                        result[j++] = row.TotalBakers;
                    break;
                case "totalDelegated":
                    foreach (var row in rows)
                        result[j++] = row.TotalDelegated;
                    break;
                case "totalDelegators":
                    foreach (var row in rows)
                        result[j++] = row.TotalDelegators;
                    break;
                case "totalStaking":
                    foreach (var row in rows)
                        result[j++] = row.TotalStaking;
                    break;
                case "selectedBakers":
                    foreach (var row in rows)
                        result[j++] = row.SelectedBakers;
                    break;
                case "selectedStake":
                    foreach (var row in rows)
                        result[j++] = row.SelectedStake;
                    break;
                case "quote":
                    foreach (var row in rows)
                        result[j++] = Quotes.Get(quote, row.LastLevel);
                    break;
            }

            return result;
        }
    }
}
