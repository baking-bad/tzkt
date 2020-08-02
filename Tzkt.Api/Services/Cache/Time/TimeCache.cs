using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Dapper;

namespace Tzkt.Api.Services.Cache
{
    public class TimeCache : DbConnection
    {
        readonly static SemaphoreSlim Sema = new SemaphoreSlim(1, 1);

        readonly List<DateTime> Times;
        readonly ILogger Logger;

        public TimeCache(IConfiguration config, ILogger<TimeCache> logger) : base(config)
        {
            Logger = logger;

            Logger.LogDebug("Initializing time cache...");

            var sql = @"
                SELECT    ""Timestamp""
                FROM      ""Blocks""
                ORDER BY  ""Level""";

            using var db = GetConnection();
            Times = db.Query<DateTime>(sql).ToList();

            Logger.LogDebug($"Time cache initialized with {Times.Count} items");
        }

        public int Count => Times.Count;

        public DateTime this[int level]
        {
            get
            {
                if (Times.Count <= level)
                {
                    Sema.Wait();

                    if (Times.Count <= level)
                        Update();

                    Sema.Release();
                }
                
                return Times[level];
            }
        }

        public int FindLevel(DateTime datetime, Nearest mode)
        {
            if (Times.Count == 0)
                return -1;

            if (datetime > Times[^1])
                return mode == Nearest.Lower ? Times.Count - 1 : -1;

            if (datetime < Times[0])
                return mode == Nearest.Higher ? 0 : -1;

            #region binary search
            var from = 0;
            var mid = 0;
            var to = Times.Count - 1;

            while (from <= to)
            {
                mid = from + (to - from) / 2;

                if (datetime > Times[mid])
                {
                    from = mid + 1;
                }
                else if (datetime < Times[mid])
                {
                    to = mid - 1;
                }
                else
                {
                    return mid;
                }
            }

            return mode == Nearest.Higher ? from : to;
            #endregion
        }

        public void Update()
        {
            var sql = @"
                SELECT    ""Timestamp""
                FROM      ""Blocks""
                WHERE     ""Level"" >= @fromLevel
                ORDER BY  ""Level""";

            using var db = GetConnection();
            var items = db.Query<DateTime>(sql, new { fromLevel = Times.Count });

            Times.AddRange(items);
        }

        public async Task UpdateAsync()
        {
            await Sema.WaitAsync();

            var sql = @"
                SELECT    ""Timestamp""
                FROM      ""Blocks""
                WHERE     ""Level"" >= @fromLevel
                ORDER BY  ""Level""";

            using var db = GetConnection();
            var items = await db.QueryAsync<DateTime>(sql, new { fromLevel = Times.Count });

            Times.AddRange(items);

            Sema.Release();
        }
    }

    public enum Nearest
    {
        Lower,
        Higher
    }

    public static class TimeCacheExt
    {
        public static void AddTimeCache(this IServiceCollection services)
        {
            services.AddSingleton<TimeCache>();
        }
    }
}
