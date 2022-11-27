using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
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
            Current = db.QueryFirst<RawState>(StateSql);
            logger.LogInformation("Loaded state [{level}:{hash}]", Current.Level, Current.Hash);
        }

        public void Update(RawState newState, List<(int Level, string Hash)> changes)
        {
            Logger.LogDebug("Updating state cache with {cnt} changes...", changes.Count);

            var validLevel = Current.Level;
            foreach (var (level, _) in changes.Where(x => x.Level < Current.Level))
                validLevel = Math.Min(validLevel, level);

            Reorganized = validLevel != Current.Level;
            ValidLevel = validLevel;
            Current = newState;

            if (Reorganized) Logger.LogDebug("Reorg after block {level} detected", validLevel);
            Logger.LogDebug("New state [{level}:{hash}]", Current.Level, Current.Hash);
        }

        public void UpdateSyncState(int knownHead, DateTime lastSync)
        {
            Current.KnownHead = knownHead;
            Current.LastSync = lastSync;
        }

        public async Task<RawState> LoadAsync()
        {
            using var db = GetConnection();
            return await db.QueryFirstAsync<RawState>(StateSql);
        }
    }
}
