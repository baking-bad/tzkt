using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Dapper;

using Tzkt.Data.Models;

namespace Tzkt.Api.Services.Cache
{
    public class StateCache : DbConnection
    {
        AppState State;

        readonly CacheConfig Config;
        readonly ILogger Logger;

        public StateCache(IConfiguration config, ILogger<StateCache> logger) : base(config)
        {
            Config = config.GetCacheConfig();
            Logger = logger;

            Logger.LogDebug("Initializing state cache...");

            var sql = @"
                SELECT  ""Level"", ""Hash"", ""Timestamp"", ""ManagerCounter""
                FROM    ""AppState""
                LIMIT   1";

            using var db = GetConnection();
            State = db.QueryFirst<AppState>(sql);

            Logger.LogDebug($"State cache initialized at {State.Level} level");
        }

        public AppState GetState() => State;

        public int GetLevel() => State.Level;

        public int GetCounter() => State.ManagerCounter;

        public void Update(AppState state) => State = state;
    }

    public static class StateCacheExt
    {
        public static void AddStateCache(this IServiceCollection services)
        {
            services.AddSingleton<StateCache>();
        }
    }
}
