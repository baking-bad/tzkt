using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tzkt.Data.Models;

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
            try
            {
                Logger.LogWarning("Observer started");

                #region init state
                if (!await ResetState(cancelToken)) return;
                Logger.LogInformation($"State initialized: [{AppState.Level}:{AppState.Hash}]");
                #endregion

                #region init quotes
                await InitQuotes();
                Logger.LogInformation($"Quotes initialized: [{AppState.QuoteLevel}]");
                #endregion

                Logger.LogInformation("Synchronization started");

                while (!cancelToken.IsCancellationRequested)
                {
                    #region wait for updates
                    try
                    {
                        if (!await WaitForUpdatesAsync(cancelToken)) break;
                        var head = await Node.GetHeaderAsync();
                        Logger.LogDebug($"New head is found [{head.Level}:{head.Hash}]");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Failed to check updates. {ex.Message}");
                        await Task.Delay(3000, CancellationToken.None);
                        continue;
                    }
                    #endregion

                    #region apply updates
                    try
                    {
                        if (!await ApplyUpdatesAsync(cancelToken)) break;
                        Logger.LogDebug($"Current head [{AppState.Level}:{AppState.Hash}]");
                    }
                    catch (BaseException ex) when (ex.RebaseRequired)
                    {
                        Logger.LogError($"Failed to apply block: {ex.Message}. Rebase local branch...");
                        if (!await ResetState(cancelToken)) break;
                         
                        try
                        {
                            if (!await RebaseLocalBranchAsync(cancelToken)) break;
                        }
                        catch (Exception exx)
                        {
                            Logger.LogError($"Failed to rebase branch. {exx.Message}");
                            await Task.Delay(3000, CancellationToken.None);
                            if (!await ResetState(cancelToken)) break;
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Failed to apply updates. {ex.Message}");
                        await Task.Delay(3000, CancellationToken.None);
                        if (!await ResetState(cancelToken)) break;
                        continue;
                    }
                    #endregion
                }
            }
            catch (Exception ex)
            {
                // should never get here
                Logger.LogCritical($"Observer crashed: {ex.Message}");
            }
            finally
            {
                Logger.LogWarning("Observer stopped");
            }
        }

        private async Task<bool> ResetState(CancellationToken cancelToken)
        {
            while (!cancelToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = Services.CreateScope();
                    var cache = scope.ServiceProvider.GetRequiredService<CacheService>();

                    await cache.ResetAsync();
                    AppState = cache.AppState.Get();
                    break;
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Failed to reset state. {ex.Message}");
                    await Task.Delay(1000, CancellationToken.None);
                }
            }
            return !cancelToken.IsCancellationRequested;
        }

        private async Task InitQuotes()
        {
            using var scope = Services.CreateScope();
            var quotes = scope.ServiceProvider.GetRequiredService<QuotesService>();
            await quotes.Init();
        }

        private async Task<bool> WaitForUpdatesAsync(CancellationToken cancelToken)
        {
            while (!await Node.HasUpdatesAsync(AppState.Level))
            {
                if (cancelToken.IsCancellationRequested)
                    return false;

                await Task.Delay(1000, CancellationToken.None);
            }
            return true;
        }

        private async Task<bool> RebaseLocalBranchAsync(CancellationToken cancelToken)
        {
            while (AppState.Level >= 0 && !cancelToken.IsCancellationRequested)
            {
                var header = await Node.GetHeaderAsync(AppState.Level);
                if (AppState.Hash == header.Hash) break;

                Logger.LogError($"Invalid head [{AppState.Level}:{AppState.Hash}]. Reverting...");

                using var scope = Services.CreateScope();
                var protoHandler = scope.ServiceProvider.GetProtocolHandler(AppState.Level, AppState.Protocol);
                AppState = await protoHandler.RevertLastBlock(header.Predecessor);

                Logger.LogInformation($"Reverted to [{AppState.Level}:{AppState.Hash}]");
            }

            return !cancelToken.IsCancellationRequested;
        }

        private async Task<bool> ApplyUpdatesAsync(CancellationToken cancelToken)
        {
            while (!cancelToken.IsCancellationRequested)
            {
                var sync = DateTime.UtcNow;
                var header = await Node.GetHeaderAsync();
                if (AppState.Level == header.Level) break;

                //if (AppState.Level >= 0)
                //    throw new ValidationException("Test", true);

                Logger.LogDebug($"Applying block...");

                using var scope = Services.CreateScope();
                var protocol = scope.ServiceProvider.GetProtocolHandler(AppState.Level + 1, AppState.NextProtocol);
                AppState = await protocol.CommitBlock(header.Level, sync);

                Logger.LogInformation($"Applied {AppState.Level} of {AppState.KnownHead}");
            }

            return !cancelToken.IsCancellationRequested;
        }
    }
}
