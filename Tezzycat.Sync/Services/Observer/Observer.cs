using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

using Tezzycat.Data;

namespace Tezzycat.Sync.Services
{
    public class Observer : BackgroundService
    {
        private readonly TezosNode Node;
        private readonly IServiceScopeFactory Services;
        private readonly ILogger Logger;

        public Observer(TezosNode node, IServiceScopeFactory services, ILogger<Observer> logger)
        {
            Node = node;
            Services = services;
            Logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Logger.LogWarning("Observer is started");

            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = Services.CreateScope())
                {
                    var db = scope.ServiceProvider.GetService<SyncContext>();
                    var state = await db.AppState.FirstOrDefaultAsync();

                    #region check for updates
                    try
                    {
                        while (!await Node.HasUpdatesAsync(state.Level))
                            await Task.Delay(1000);

                        Logger.LogDebug($"Newer head is found: {(await Node.GetHeaderAsync()).Level}");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogCritical($"Failed to check updates. {ex.Message}");
                        await Task.Delay(5000);
                        continue;
                    }
                    #endregion

                    #region validate current branch
                    try
                    {
                        while (state.Level >= 0
                            && !await Node.ValidateBranchAsync(state.Level, state.Hash))
                        {
                            Logger.LogError($"Invalid branch: {state.Level} - {state.Hash}. Reverting block...");

                            var protoHandler = scope.ServiceProvider.GetProtocolHandler(state.Protocol);
                            state = await protoHandler.RevertLastBlock();

                            Logger.LogDebug($"Reverted to: {state.Level} - {state.Hash}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogCritical($"Failed to validate branch. {ex.Message}");
                        await Task.Delay(5000);
                        continue;
                    }
                    #endregion

                    #region apply updates
                    try
                    {
                        while (await Node.HasUpdatesAsync(state.Level))
                        {
                            Logger.LogDebug($"Applying block {state.Level + 1}...");

                            var block = await Node.Rpc.Blocks[state.Level + 1].GetAsync();
                            var protoHandler = scope.ServiceProvider.GetProtocolHandler(block["protocol"].String());
                            state = await protoHandler.ApplyBlock(block);

                            Logger.LogDebug($"New head: {state.Level} - {state.Hash}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogCritical($"Failed to apply updates. {ex.Message}");
                        await Task.Delay(5000);
                        continue;
                    }
                    #endregion
                }
            }

            Logger.LogWarning("Observer is stoped");
        }
    }
}
