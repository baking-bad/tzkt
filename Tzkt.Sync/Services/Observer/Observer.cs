using App.Metrics;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Services
{
    public class Observer : BackgroundService
    {
        public AppState AppState { get; private set; }

        readonly TezosNode Node;
        readonly IServiceScopeFactory Services;
        readonly ILogger Logger;
        readonly IMetrics Metrics;

        public Observer(TezosNode node, IServiceScopeFactory services, ILogger<Observer> logger, IMetrics metrics)
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
                await InitQuotes();
                Logger.LogInformation("Quotes initialized: [{level}]", AppState.QuoteLevel);
                #endregion

                Logger.LogInformation("Synchronization started");

                while (!cancelToken.IsCancellationRequested)
                {
                    #region wait for updates
                    try
                    {
                        if (!await WaitForUpdatesAsync(cancelToken)) break;
                        var head = await Node.GetHeaderAsync();
                        Logger.LogDebug("New head is found [{level}:{hash}]", head.Level, head.Hash);
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
                        Logger.LogDebug("Current head [{level}:{hash}]", AppState.Level, AppState.Hash);
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
            while (!cancelToken.IsCancellationRequested)
            {
                var header = await Node.GetHeaderAsync();
                if (AppState.Level == header.Level) break;

                //if (AppState.Level >= 0)
                //    throw new ValidationException("Test", true);

                Logger.LogDebug($"Applying block...");
                using (Metrics.Measure.Timer.Time(MetricsRegistry.ApplyBlockTime))
                {
                    using var scope = Services.CreateScope();
                    var protocol = scope.ServiceProvider.GetProtocolHandler(AppState.Level + 1, AppState.NextProtocol);
                    AppState = await protocol.CommitBlock(header.Level);
                }
                Logger.LogInformation("Applied {level} of {total}", AppState.Level, AppState.KnownHead);
            }

            return !cancelToken.IsCancellationRequested;
        }
    }
}
