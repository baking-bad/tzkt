using App.Metrics;
using Mvkt.Data.Models;

namespace Mvkt.Sync.Services
{
    public class Observer : BackgroundService
    {
        public AppState AppState { get; private set; }

        readonly MavrykNode Node;
        readonly IServiceScopeFactory Services;
        readonly ILogger Logger;
        readonly IMetrics Metrics;

        public Observer(MavrykNode node, IServiceScopeFactory services, ILogger<Observer> logger, IMetrics metrics)
        {
            Node = node;
            Services = services;
            Logger = logger;
            Metrics = metrics;
        }

        protected override async Task ExecuteAsync(CancellationToken cancelToken)
        {
            try
            {
                Logger.LogWarning("Observer started");

                #region init state
                if (!await ResetState(cancelToken)) return;
                Logger.LogInformation("State initialized: [{level}:{hash}]", AppState.Level, AppState.Hash);
                #endregion

                #region init quotes
                // Initialize quotes in background to avoid blocking block synchronization.
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await InitQuotes();
                        Logger.LogInformation("Quotes initialized: [{level}]", AppState.QuoteLevel);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning(ex, "Failed to initialize quotes, will retry during sync");
                    }
                });
                #endregion

                Logger.LogInformation("Synchronization started");

                while (!cancelToken.IsCancellationRequested)
                {
                    #region wait for updates
                    try
                    {
                        // Check if we're behind - if so, skip waiting and process immediately
                        var head = await Node.GetHeaderAsync();
                        if (AppState.Level < head.Level)
                        {
                            // We're behind, process blocks immediately without waiting
                        }
                        else
                        {
                            // We're caught up, wait for new blocks
                            if (!await WaitForUpdatesAsync(cancelToken)) break;
                            head = await Node.GetHeaderAsync();
                        }
                        
                        if (head.Level == AppState.Level + 1)
                        {
                            Metrics.Measure.Histogram.Update(MetricsRegistry.BlockAppearanceDelay,
                                (long)(DateTime.UtcNow - head.Timestamp).TotalMilliseconds);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Failed to check updates");
                        await Task.Delay(3000, CancellationToken.None);
                        continue;
                    }
                    #endregion

                    #region apply updates
                    try
                    {
                        if (!await ApplyUpdatesAsync(cancelToken)) break;
                    }
                    catch (InvalidOperationException ex) when (ex.Message.Contains("Block not found"))
                    {
                        // Block not found - wait and retry, don't reset state
                        await Task.Delay(100, CancellationToken.None);
                        continue;
                    }
                    catch (BaseException ex) when (ex.RebaseRequired)
                    {
                        Logger.LogError(ex, "Failed to apply block. Rebase local branch...");
                        if (!await ResetState(cancelToken)) break;
                         
                        try
                        {
                            if (!await RebaseLocalBranchAsync(cancelToken)) break;
                        }
                        catch (Exception exx)
                        {
                            Logger.LogError(exx, "Failed to rebase branch");
                            await Task.Delay(3000, CancellationToken.None);
                            if (!await ResetState(cancelToken)) break;
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Failed to apply updates");
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
                Logger.LogCritical(ex, "Observer crashed");
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
                    Logger.LogError(ex, "Failed to reset state");
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
            // Use shorter delay during sync to check more frequently
            var consecutiveChecks = 0;
            while (!await Node.HasUpdatesAsync(AppState.Level))
            {
                if (cancelToken.IsCancellationRequested)
                    return false;

                // First check is immediate, then use short delays
                if (consecutiveChecks++ > 0)
                {
                    await Task.Delay(100, CancellationToken.None);
                }
            }
            return true;
        }

        private async Task<bool> RebaseLocalBranchAsync(CancellationToken cancelToken)
        {
            while (AppState.Level >= 0 && !cancelToken.IsCancellationRequested)
            {
                var header = await Node.GetHeaderAsync(AppState.Level);
                if (AppState.Hash == header.Hash) break;

                Logger.LogError("Invalid head [{level}:{hash}]. Reverting...", AppState.Level, AppState.Hash);
                using (Metrics.Measure.Timer.Time(MetricsRegistry.RevertBlockTime))
                {
                    using var scope = Services.CreateScope();
                    var protoHandler = scope.ServiceProvider.GetProtocolHandler(AppState.Level, AppState.Protocol);
                    AppState = await protoHandler.RevertLastBlock(header.Predecessor);
                }
                Logger.LogInformation("Reverted to [{level}:{hash}]", AppState.Level, AppState.Hash);
            }

            return !cancelToken.IsCancellationRequested;
        }

        private async Task<bool> ApplyUpdatesAsync(CancellationToken cancelToken)
        {
            var lastLoggedLevel = AppState.Level;
            var logInterval = 100; // Log every 100 blocks to reduce I/O overhead
            var headerCache = await Node.GetHeaderAsync();
            var headerCacheTime = DateTime.UtcNow;
            var headerCacheLevel = AppState.Level;
            
            while (!cancelToken.IsCancellationRequested)
            {
                // Refresh header cache periodically (every 10 blocks or every 500ms)
                var blocksSinceUpdate = AppState.Level - headerCacheLevel;
                var timeSinceUpdate = (DateTime.UtcNow - headerCacheTime).TotalMilliseconds;
                if (blocksSinceUpdate >= 10 || timeSinceUpdate > 500)
                {
                    headerCache = await Node.GetHeaderAsync();
                    headerCacheTime = DateTime.UtcNow;
                    headerCacheLevel = AppState.Level;
                }
                
                if (AppState.Level == headerCache.Level) break;

                // Check if the next block exists before trying to fetch it
                var nextLevel = AppState.Level + 1;
                if (nextLevel > headerCache.Level)
                {
                    // Next block doesn't exist yet, refresh header and wait briefly
                    await Task.Delay(100, CancellationToken.None);
                    headerCache = await Node.GetHeaderAsync();
                    headerCacheTime = DateTime.UtcNow;
                    headerCacheLevel = AppState.Level;
                    continue;
                }

                //if (AppState.Level >= 0)
                //    throw new ValidationException("Test", true);

                using (Metrics.Measure.Timer.Time(MetricsRegistry.ApplyBlockTime))
                {
                    using var scope = Services.CreateScope();
                    var protocol = scope.ServiceProvider.GetProtocolHandler(AppState.Level + 1, AppState.NextProtocol);
                    AppState = await protocol.CommitBlock(headerCache.Level);
                }
                
                // Log progress less frequently to reduce I/O overhead
                var blocksProcessed = AppState.Level - lastLoggedLevel;
                if (blocksProcessed >= logInterval || AppState.Level == AppState.KnownHead)
                {
                    var percent = AppState.KnownHead > 0 
                        ? (double)AppState.Level / AppState.KnownHead * 100 
                        : 0;
                    Logger.LogInformation("Applied {level} of {total} ({percent:F2}%)", 
                        AppState.Level, AppState.KnownHead, percent);
                    lastLoggedLevel = AppState.Level;
                }
            }

            return !cancelToken.IsCancellationRequested;
        }
    }
}

