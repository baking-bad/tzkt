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
    public class StateCache : DbConnection
    {
        RawState State;

        readonly CacheConfig Config;
        readonly ILogger Logger;

        public StateCache(IConfiguration config, ILogger<StateCache> logger) : base(config)
        {
            Config = config.GetCacheConfig();
            Logger = logger;

            Logger.LogDebug("Initializing state cache...");

            State = LoadState();

            Logger.LogDebug($"State cache initialized at {State.Level} level");
        }

        public RawState GetState() => State;

        public RawState LoadState()
        {
            var sql = @"
                SELECT  ""KnownHead"", ""LastSync"", ""Level"", ""Hash"", ""Protocol"", ""Timestamp"", ""ManagerCounter""
                FROM    ""AppState""
                LIMIT   1";

            using var db = GetConnection();
            return db.QueryFirst<RawState>(sql);
        }

        public async Task<RawState> LoadStateAsync()
        {
            var sql = @"
                SELECT  ""KnownHead"", ""LastSync"", ""Level"", ""Hash"", ""Protocol"", ""Timestamp"", ""ManagerCounter""
                FROM    ""AppState""
                LIMIT   1";

            using var db = GetConnection();
            return await db.QueryFirstAsync<RawState>(sql);
        }

        public int GetLevel() => State.Level;

        public int GetCounter() => State.ManagerCounter;

        public void Update(RawState state) => State = state;
    }

    public static class StateCacheExt
    {
        public static void AddStateCache(this IServiceCollection services)
        {
            services.AddSingleton<StateCache>();
        }
    }
}
