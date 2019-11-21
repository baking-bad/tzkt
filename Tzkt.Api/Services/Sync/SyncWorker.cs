using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using Dapper;

using Tzkt.Api.Services.Cache;
using Tzkt.Data.Models;
using Tzkt.Api.Utils;

namespace Tzkt.Api.Services.Sync
{
    public class SyncWorker : BackgroundService
    {
        readonly SyncConfig Config;
        readonly string ConnectionString;
        readonly AccountsCache Accounts;
        readonly AliasService Aliases;
        readonly StateCache State;
        readonly ILogger Logger;

        int BlocksTime;
        AppState LastState;
        DateTime NextSyncTime;

        public SyncWorker(AccountsCache accounts, AliasService aliases, StateCache state, IConfiguration config, ILogger<SyncWorker> logger)
        {
            Config = config.GetSyncConfig();
            ConnectionString = config.GetConnectionString("DefaultConnection");
            Accounts = accounts;
            Aliases = aliases;
            State = state;
            Logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            #region init state
            Logger.LogDebug("Initializing sync worker...");
            
            BlocksTime = await GetBlocksTime();
            LastState = await GetCurrentState();
            NextSyncTime = DateTime.UtcNow.AddSeconds(Config.CheckInterval);

            Logger.LogInformation($"Sync worker initialized with level {LastState.Level} and blocks time {BlocksTime}s");
            #endregion

            Logger.LogInformation("Syncronization started");

            while (!stoppingToken.IsCancellationRequested)
            {
                if (DateTime.UtcNow > NextSyncTime)
                {
                    var currentState = await GetCurrentState();
                    if (currentState.Hash != LastState.Hash)
                    {
                        Logger.LogDebug($"New state detected [{currentState.Level}:{currentState.Hash}]");

                        var updateLevel = await IsStateValid(LastState)
                            ? LastState.Level + 1
                            : Math.Min(LastState.Level, currentState.Level) - 5;

                        Logger.LogDebug($"Updating cache from {updateLevel} level...");

                        var changedAccounts = await Accounts.Update(updateLevel);

                        Aliases.Update(changedAccounts);

                        State.Update(currentState);

                        LastState = currentState;

                        Logger.LogDebug("State updated");
                    }

                    NextSyncTime = DateTimeExt.Min(
                        DateTimeExt.Max(currentState.Timestamp.AddSeconds(BlocksTime), DateTime.UtcNow.AddSeconds(Config.UpdateInterval)),
                        DateTime.UtcNow.AddSeconds(Config.CheckInterval));
                }

                await Task.Delay(500);
            }

            Logger.LogInformation("Syncronization stoped");
        }

        IDbConnection GetConnection() => new NpgsqlConnection(ConnectionString);

        async Task<bool> IsStateValid(AppState state)
        {
            var sql = @"
                SELECT  ""Hash""
                FROM    ""Blocks""
                WHERE   ""Level"" = @level
                LIMIT   1";

            using var db = GetConnection();
            var hash = await db.QueryFirstOrDefaultAsync<string>(sql, new { level = state.Level });

            return state.Hash == hash;
        }

        async Task<AppState> GetCurrentState()
        {
            var sql = @"
                SELECT  ""Level"", ""Hash"", ""Timestamp"", ""ManagerCounter""
                FROM    ""AppState""
                LIMIT   1";

            using var db = GetConnection();
            return await db.QueryFirstAsync<AppState>(sql);
        }

        async Task<int> GetBlocksTime()
        {
            var sql = @"
                SELECT   ""TimeBetweenBlocks""
                FROM     ""Protocols""
                ORDER BY ""Code"" DESC
                LIMIT    1";

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql);
        }
    }

    public static class SyncWorkerExt
    {
        public static void AddSynchronization(this IServiceCollection services)
        {
            services.AddHostedService<SyncWorker>();
        }
    }
}
