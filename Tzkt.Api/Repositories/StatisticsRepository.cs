using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Dapper;

using Tzkt.Api.Models;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Repositories
{
    public class StatisticsRepository : DbConnection
    {
        readonly TimeCache Time;
        readonly QuotesCache Quotes;

        public StatisticsRepository(TimeCache time, QuotesCache quotes, IConfiguration config) : base(config)
        {
            Time = time;
            Quotes = quotes;
        }

        public async Task<IEnumerable<Statistics>> Get(
            StatisticsPeriod period,
            Int32Parameter cycle,
            Int32Parameter level,
            TimestampParameter timestamp,
            DateTimeParameter date,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            Symbols quote)
        {
            var sql = new SqlBuilder(@"SELECT * FROM ""Statistics""");

            if (period == StatisticsPeriod.Cyclic)
                sql.Filter(@"""Cycle"" IS NOT NULL");
            else if (period == StatisticsPeriod.Daily)
                sql.Filter(@"""Date"" IS NOT NULL");

            sql.Filter("Cycle", cycle)
                .Filter("Level", level)
                .Filter("Level", timestamp)
                .Filter("Date", date)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Level", "Level"),
                    "cycle" => ("Cycle", "Cycle"),
                    "date" => ("Date", "Date"),
                    _ => ("Id", "Id")
                });

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row => new Statistics
            {
                Cycle = row.Cycle,
                Date = row.Date,
                Level = row.Level,
                Timestamp = Time[row.Level],
                TotalBootstrapped = row.TotalBootstrapped,
                TotalCommitments = row.TotalCommitments,
                TotalCreated = row.TotalCreated,
                TotalBurned = row.TotalBurned,
                TotalBanished = row.TotalBanished,
                TotalActivated = row.TotalActivated,
                TotalFrozen = row.TotalFrozen,
                TotalRollupBonds = row.TotalRollupBonds,
                TotalSupply = row.TotalBootstrapped + row.TotalCommitments + row.TotalCreated - row.TotalBurned - row.TotalBanished,
                CirculatingSupply = row.TotalBootstrapped + row.TotalActivated + row.TotalCreated - row.TotalBurned - row.TotalBanished - row.TotalFrozen,
                Quote = Quotes.Get(quote, row.Level),
            });
        }

        public async Task<object[][]> Get(
            StatisticsPeriod period,
            Int32Parameter cycle,
            Int32Parameter level,
            TimestampParameter timestamp,
            DateTimeParameter date,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string[] fields,
            Symbols quote)
        {
            var columns = new HashSet<string>(fields.Length + 8);
            foreach (var field in fields)
            {
                switch (field)
                {
                    case "cycle": columns.Add(@"""Cycle"""); break;
                    case "date": columns.Add(@"""Date"""); break;
                    case "level": columns.Add(@"""Level"""); break;
                    case "timestamp": columns.Add(@"""Level"""); break;
                    case "totalBootstrapped": columns.Add(@"""TotalBootstrapped"""); break;
                    case "totalCommitments": columns.Add(@"""TotalCommitments"""); break;
                    case "totalCreated": columns.Add(@"""TotalCreated"""); break;
                    case "totalBurned": columns.Add(@"""TotalBurned"""); break;
                    case "totalBanished": columns.Add(@"""TotalBanished"""); break;
                    case "totalActivated": columns.Add(@"""TotalActivated"""); break;
                    case "totalFrozen": columns.Add(@"""TotalFrozen"""); break;
                    case "totalRollupBonds": columns.Add(@"""TotalRollupBonds"""); break;
                    case "totalSupply":
                        columns.Add(@"""TotalBootstrapped""");
                        columns.Add(@"""TotalCommitments""");
                        columns.Add(@"""TotalCreated""");
                        columns.Add(@"""TotalBurned""");
                        columns.Add(@"""TotalBanished""");
                        break;
                    case "circulatingSupply":
                        columns.Add(@"""TotalBootstrapped""");
                        columns.Add(@"""TotalActivated""");
                        columns.Add(@"""TotalCreated""");
                        columns.Add(@"""TotalBurned""");
                        columns.Add(@"""TotalBanished""");
                        columns.Add(@"""TotalFrozen""");
                        break;
                    case "quote": columns.Add(@"""Level"""); break;
                }
            }

            if (columns.Count == 0)
                return Array.Empty<object[]>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""Statistics""");

            if (period == StatisticsPeriod.Cyclic)
                sql.Filter(@"""Cycle"" IS NOT NULL");
            else if (period == StatisticsPeriod.Daily)
                sql.Filter(@"""Date"" IS NOT NULL");

            sql.Filter("Cycle", cycle)
                .Filter("Level", level)
                .Filter("Level", timestamp)
                .Filter("Date", date)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Level", "Level"),
                    "cycle" => ("Cycle", "Cycle"),
                    "date" => ("Date", "Date"),
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
                    case "cycle":
                        foreach (var row in rows)
                            result[j++][i] = row.Cycle;
                        break;
                    case "date":
                        foreach (var row in rows)
                            result[j++][i] = row.Date;
                        break;
                    case "level":
                        foreach (var row in rows)
                            result[j++][i] = row.Level;
                        break;
                    case "timestamp":
                        foreach (var row in rows)
                            result[j++][i] = Time[row.Level];
                        break;
                    case "totalBootstrapped":
                        foreach (var row in rows)
                            result[j++][i] = row.TotalBootstrapped;
                        break;
                    case "totalCommitments":
                        foreach (var row in rows)
                            result[j++][i] = row.TotalCommitments;
                        break;
                    case "totalCreated":
                        foreach (var row in rows)
                            result[j++][i] = row.TotalCreated;
                        break;
                    case "totalBurned":
                        foreach (var row in rows)
                            result[j++][i] = row.TotalBurned;
                        break;
                    case "totalBanished":
                        foreach (var row in rows)
                            result[j++][i] = row.TotalBanished;
                        break;
                    case "totalActivated":
                        foreach (var row in rows)
                            result[j++][i] = row.TotalActivated;
                        break;
                    case "totalFrozen":
                        foreach (var row in rows)
                            result[j++][i] = row.TotalFrozen;
                        break;
                    case "totalRollupBonds":
                        foreach (var row in rows)
                            result[j++][i] = row.TotalRollupBonds;
                        break;
                    case "totalSupply":
                        foreach (var row in rows)
                            result[j++][i] = row.TotalBootstrapped + row.TotalCommitments + row.TotalCreated - row.TotalBurned - row.TotalBanished;
                        break;
                    case "circulatingSupply":
                        foreach (var row in rows)
                            result[j++][i] = row.TotalBootstrapped + row.TotalActivated + row.TotalCreated - row.TotalBurned - row.TotalBanished - row.TotalFrozen;
                        break;
                    case "quote":
                        foreach (var row in rows)
                            result[j++][i] = Quotes.Get(quote, row.Level);
                        break;
                }
            }

            return result;
        }

        public async Task<object[]> Get(
            StatisticsPeriod period,
            Int32Parameter cycle,
            Int32Parameter level,
            TimestampParameter timestamp,
            DateTimeParameter date,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string field,
            Symbols quote)
        {
            var columns = new HashSet<string>(8);
            switch (field)
            {
                case "cycle": columns.Add(@"""Cycle"""); break;
                case "date": columns.Add(@"""Date"""); break;
                case "level": columns.Add(@"""Level"""); break;
                case "timestamp": columns.Add(@"""Level"""); break;
                case "totalBootstrapped": columns.Add(@"""TotalBootstrapped"""); break;
                case "totalCommitments": columns.Add(@"""TotalCommitments"""); break;
                case "totalCreated": columns.Add(@"""TotalCreated"""); break;
                case "totalBurned": columns.Add(@"""TotalBurned"""); break;
                case "totalBanished": columns.Add(@"""TotalBanished"""); break;
                case "totalActivated": columns.Add(@"""TotalActivated"""); break;
                case "totalFrozen": columns.Add(@"""TotalFrozen"""); break;
                case "totalRollupBonds": columns.Add(@"""TotalRollupBonds"""); break;
                case "totalSupply":
                    columns.Add(@"""TotalBootstrapped""");
                    columns.Add(@"""TotalCommitments""");
                    columns.Add(@"""TotalCreated""");
                    columns.Add(@"""TotalBurned""");
                    columns.Add(@"""TotalBanished""");
                    break;
                case "circulatingSupply":
                    columns.Add(@"""TotalBootstrapped""");
                    columns.Add(@"""TotalActivated""");
                    columns.Add(@"""TotalCreated""");
                    columns.Add(@"""TotalBurned""");
                    columns.Add(@"""TotalBanished""");
                    columns.Add(@"""TotalFrozen""");
                    break;
                case "quote": columns.Add(@"""Level"""); break;
            }

            if (columns.Count == 0)
                return Array.Empty<object>();

            var sql = new SqlBuilder($@"SELECT {string.Join(',', columns)} FROM ""Statistics""");

            if (period == StatisticsPeriod.Cyclic)
                sql.Filter(@"""Cycle"" IS NOT NULL");
            else if (period == StatisticsPeriod.Daily)
                sql.Filter(@"""Date"" IS NOT NULL");

            sql.Filter("Cycle", cycle)
                .Filter("Level", level)
                .Filter("Level", timestamp)
                .Filter("Date", date)
                .Take(sort, offset, limit, x => x switch
                {
                    "level" => ("Level", "Level"),
                    "cycle" => ("Cycle", "Cycle"),
                    "date" => ("Date", "Date"),
                    _ => ("Id", "Id")
                });

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            var result = new object[rows.Count()];
            var j = 0;

            switch (field)
            {
                case "cycle":
                    foreach (var row in rows)
                        result[j++] = row.Cycle;
                    break;
                case "date":
                    foreach (var row in rows)
                        result[j++] = row.Date;
                    break;
                case "level":
                    foreach (var row in rows)
                        result[j++] = row.Level;
                    break;
                case "timestamp":
                    foreach (var row in rows)
                        result[j++] = Time[row.Level];
                    break;
                case "totalBootstrapped":
                    foreach (var row in rows)
                        result[j++] = row.TotalBootstrapped;
                    break;
                case "totalCommitments":
                    foreach (var row in rows)
                        result[j++] = row.TotalCommitments;
                    break;
                case "totalCreated":
                    foreach (var row in rows)
                        result[j++] = row.TotalCreated;
                    break;
                case "totalBurned":
                    foreach (var row in rows)
                        result[j++] = row.TotalBurned;
                    break; 
                case "totalBanished":
                    foreach (var row in rows)
                        result[j++] = row.TotalBanished;
                    break; 
                case "totalActivated":
                    foreach (var row in rows)
                        result[j++] = row.TotalActivated;
                    break;
                case "totalFrozen":
                    foreach (var row in rows)
                        result[j++] = row.TotalFrozen;
                    break;
                case "totalRollupBonds":
                    foreach (var row in rows)
                        result[j++] = row.TotalRollupBonds;
                    break;
                case "totalSupply":
                    foreach (var row in rows)
                        result[j++] = row.TotalBootstrapped + row.TotalCommitments + row.TotalCreated - row.TotalBurned - row.TotalBanished;
                    break;
                case "circulatingSupply":
                    foreach (var row in rows)
                        result[j++] = row.TotalBootstrapped + row.TotalActivated + row.TotalCreated - row.TotalBurned - row.TotalBanished - row.TotalFrozen;
                    break;
                case "quote":
                    foreach (var row in rows)
                        result[j++] = Quotes.Get(quote, row.Level);
                    break;
            }

            return result;
        }
    }

    public enum StatisticsPeriod
    {
        None,
        Daily,
        Cyclic
    }
}
