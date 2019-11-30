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

        public TimeCache(IConfiguration config, ILogger<StateCache> logger) : base(config)
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

        public void Update()
        {
            var sql = @"
                SELECT    ""Timestamp""
                FROM      ""Blocks""
                WHERE     ""Level"" > @fromLevel
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
                WHERE     ""Level"" > @fromLevel
                ORDER BY  ""Level""";

            using var db = GetConnection();
            var items = await db.QueryAsync<DateTime>(sql, new { fromLevel = Times.Count });

            Times.AddRange(items);

            Sema.Release();
        }
    }

    public static class TimeCacheExt
    {
        public static void AddTimeCache(this IServiceCollection services)
        {
            services.AddSingleton<TimeCache>();
        }
    }
}
