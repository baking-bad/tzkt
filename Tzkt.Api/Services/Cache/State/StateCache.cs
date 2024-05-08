using Dapper;
using Npgsql;

namespace Tzkt.Api.Services.Cache
{
    public class StateCache
    {
        const string StateSql = @"SELECT * FROM ""AppState"" LIMIT 1";

        public RawState Current { get; private set; }
        public bool Reorganized { get; private set; }
        public int ValidLevel { get; private set; }

        readonly NpgsqlDataSource DataSource;
        readonly ILogger Logger;

        public StateCache(NpgsqlDataSource dataSource, ILogger<StateCache> logger)
        {
            logger.LogDebug("Initializing state cache...");
            DataSource = dataSource;
            Logger = logger;
            using var db = DataSource.OpenConnection();
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
            await using var db = await DataSource.OpenConnectionAsync();
            return await db.QueryFirstAsync<RawState>(StateSql);
        }
    }
}
