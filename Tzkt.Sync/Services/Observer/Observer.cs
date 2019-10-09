using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Tzkt.Data;
using Tzkt.Data.Models;
using Tzkt.Sync.Protocols;

namespace Tzkt.Sync.Services
{
    public class Observer : BackgroundService
    {
        public AppState AppState { get; private set; }

        private readonly TezosNode Node;
        private readonly IServiceScopeFactory Services;
        private readonly ILogger Logger;

        public Observer(TezosNode node, IServiceScopeFactory services, ILogger<Observer> logger)
        {
            Node = node;
            Services = services;
            Logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken cancelToken)
        {
            #region init state
            AppState = await ResetState();
            Logger.LogDebug($"State initialized: [{AppState.Level}:{AppState.Hash}]");
            #endregion

            Logger.LogWarning("Observer is started");

            while (!cancelToken.IsCancellationRequested)
            {
                #region wait for updates
                try
                {
                    if (!await WaitForUpdatesAsync(cancelToken))
                        break;

                    var head = await Node.GetHeaderAsync();
                    Logger.LogDebug($"New head is found [{head.Level}:{head.Hash}]");
                }
                catch (Exception ex)
                {
                    Logger.LogCritical($"Failed to check updates. {ex.Message}");
                    await Task.Delay(5000);
                    continue;
                }
                #endregion

                #region apply updates
                try
                {
                    if (!await ApplyUpdatesAsync(cancelToken))
                        break;

                    Logger.LogDebug($"Current head [{AppState.Level}:{AppState.Hash}]");
                }
                catch (Exception ex)
                {
                    Logger.LogCritical($"Failed to apply updates. {ex.Message}");
                    AppState = await ResetState();
                    await Task.Delay(5000);
                    continue;
                }
                #endregion
            }

            Logger.LogWarning("Observer is stoped");
        }

        private async Task<AppState> ResetState()
        {
            using var scope = Services.CreateScope();
            var accountsCache = scope.ServiceProvider.GetRequiredService<AccountManager>();
            var protocolsCache = scope.ServiceProvider.GetRequiredService<ProtocolManager>();
            var stateCache = scope.ServiceProvider.GetRequiredService<StateManager>();

            accountsCache.Clear(true);
            protocolsCache.Clear();
            stateCache.Clear();

            return await stateCache.GetAppStateAsync();
        }

        private async Task<bool> WaitForUpdatesAsync(CancellationToken cancelToken)
        {
            while (!await Node.HasUpdatesAsync(AppState.Level))
            {
                if (cancelToken.IsCancellationRequested)
                    return false;

                await Task.Delay(1000);
            }
            return true;
        }

        private async Task<bool> RebaseLocalBranchAsync(IServiceScope scope, CancellationToken cancelToken)
        {
            while (AppState.Level >= 0)
            //while (AppState.Level >= 0 && !await Node.ValidateBranchAsync(AppState.Level, AppState.Hash))
            {
                if (cancelToken.IsCancellationRequested)
                    return false;

                Logger.LogError($"Invalid head [{AppState.Level}:{AppState.Hash}]. Reverting...");

                var protoHandler = scope.ServiceProvider.GetProtocolHandler(AppState.Protocol);
                AppState = await protoHandler.RevertLastBlock();

                Logger.LogDebug($"Reverted to [{AppState.Level}:{AppState.Hash}]");
            }
            return true;
        }

        private async Task<bool> ApplyUpdatesAsync(CancellationToken cancelToken)
        {
            using (var scope = Services.CreateScope())
            {
                while (await Node.HasUpdatesAsync(AppState.Level))
                {
                    if (cancelToken.IsCancellationRequested)
                        return false;

                    Logger.LogDebug($"Loading block {AppState.Level + 1}...");
                    using var blockStream = await Node.GetBlockAsync(AppState.Level + 1);

                    try
                    {
                        if (AppState.Level == 108)
                            throw new ValidationException("Test", true);

                        Logger.LogDebug($"Applying block...");
                        var protocol = scope.ServiceProvider.GetProtocolHandler(AppState.NextProtocol);
                        AppState = await protocol.ApplyBlock(blockStream);
                        Logger.LogDebug($"Applied");
                    }
                    catch (BaseException ex) when (ex.RebaseRequired)
                    {
                        Logger.LogError($"Failed to apply block: {ex.Message}. Rebase local branch...");

                        if (!await RebaseLocalBranchAsync(scope, cancelToken))
                            return false;

                        continue;
                    }
                }
            }
            return true;
        }
    }
}
