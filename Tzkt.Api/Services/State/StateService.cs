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

namespace Tzkt.Api.Services
{
    public class StateService : DbConnection
    {
        AppState AppState;
        DateTime NextSyncTime;
        int Attempts;

        readonly AliasService Aliases;
        readonly ILogger Logger;
        readonly int TimeBetweenBlocks;
        readonly static SemaphoreSlim Sema = new SemaphoreSlim(1, 1);

        public StateService(AliasService aliases, IConfiguration config, ILogger<StateService> logger) : base(config)
        {
            Aliases = aliases;
            Logger = logger;

            logger.LogDebug("Loading protocol constants...");

            var sql = @"
                SELECT   ""TimeBetweenBlocks""
                FROM     ""Protocols""
                ORDER BY ""Code"" DESC
                LIMIT    1";

            using var db = GetConnection();
            TimeBetweenBlocks = db.QueryFirst<int>(sql);

            logger.LogInformation($"State service initialized with interval {TimeBetweenBlocks} sec");

            AppState = LoadState();
            NextSyncTime = AppState.Timestamp.AddSeconds(TimeBetweenBlocks);

            logger.LogInformation($"Current state: {AppState.Level}");
        }

        public async Task<AppState> GetState()
        {
            if (DateTime.UtcNow > NextSyncTime)
            {
                await Sema.WaitAsync();
                if (DateTime.UtcNow > NextSyncTime) // it's fine
                {
                    var newState = await LoadStateAsync();

                    NextSyncTime = newState.Level != AppState.Level
                        ? newState.Timestamp.AddSeconds(TimeBetweenBlocks)
                        : DateTime.UtcNow.AddSeconds(Attempts++ < 10 ? 1 : 10);

                    if (newState.Level != AppState.Level)
                    {
                        Logger.LogDebug($"New state detected: {AppState.Level} -> {newState.Level}");

                        var changedAccounts = await LoadChangedAccounts(AppState.Level);

                        Logger.LogDebug($"Changed accounts: {changedAccounts.Count()}");

                        Aliases.Update(changedAccounts);

                        AppState = newState;
                        Attempts = 0;

                        Logger.LogDebug("State updated");
                    }
                }
                Sema.Release();
            }

            return AppState;
        }

        public async Task<int> GetCounter()
        {
            return (await GetState()).ManagerCounter;
        }

        AppState LoadState()
        {
            var sql = @"
                SELECT  ""Level"", ""Hash"", ""Timestamp"", ""ManagerCounter""
                FROM    ""AppState""
                LIMIT   1";

            using var db = GetConnection();
            return db.QueryFirst<AppState>(sql);
        }

        async Task<AppState> LoadStateAsync()
        {
            var sql = @"
                SELECT  ""Level"", ""Hash"", ""Timestamp"", ""ManagerCounter""
                FROM    ""AppState""
                LIMIT   1";

            using var db = GetConnection();
            return await db.QueryFirstAsync<AppState>(sql);
        }

        async Task<IEnumerable<(int, string)>> LoadChangedAccounts(int fromLevel)
        {
            var sql = @"
                SELECT  ""Id"", ""Address""
                FROM    ""Accounts""
                WHERE   ""LastLevel"" > @fromLevel";

            using var db = GetConnection();
            return await db.QueryAsync<(int Id, string Address)>(sql, new { fromLevel });
        }
    }

    public static class StateServiceExt
    {
        public static void AddStateService(this IServiceCollection services)
        {
            services.AddSingleton<StateService>();
        }
    }
}
