using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Dapper;

namespace Tzkt.Api.Services.Cache
{
    public class StateCache : DbConnection
    {
        const string StateSql = @"SELECT * FROM ""AppState"" LIMIT 1";

        public RawState Current { get; private set; }
        public bool Reorganized { get; private set; }
        public int ValidLevel { get; private set; }

        readonly ILogger Logger;

        public StateCache(IConfiguration config, ILogger<StateCache> logger) : base(config)
        {
            logger.LogDebug("Initializing state cache...");
            Logger = logger;
            using var db = GetConnection();
            Current =  db.QueryFirst<RawState>(StateSql);
            logger.LogInformation("Loaded state [{1}:{2}]", Current.Level, Current.Hash);
        }

        public void Update(RawState newState, List<(int Level, string Hash)> changes)
        {
            Logger.LogDebug("Updating state cache with {1} changes...", changes.Count);

            var validLevel = Current.Level;
            foreach (var (level, _) in changes.Where(x => x.Level <= Current.Level && x.Hash != Current.Hash))
                validLevel = Math.Min(validLevel, level - 1);

            Reorganized = validLevel != Current.Level;
            ValidLevel = validLevel;
            Current = newState;

            if (Reorganized) Logger.LogDebug("Reorg after block #{1} detected", validLevel);
            Logger.LogDebug("New state [{1}:{2}]", Current.Level, Current.Hash);
        }

        public async Task<RawState> LoadAsync()
        {
            using var db = GetConnection();
            return await db.QueryFirstAsync<RawState>(StateSql);
        }
    }

    public static class StateCacheExt
    {
        public static void AddStateCache(this IServiceCollection services)
        {
            services.AddSingleton<StateCache>();
        }
    }
}
