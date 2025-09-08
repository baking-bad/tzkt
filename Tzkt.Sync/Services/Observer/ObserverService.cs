using Microsoft.EntityFrameworkCore;
using App.Metrics;
using Tzkt.Data;
using Tzkt.Data.Models;
using Tzkt.Sync.Services.Observer;

namespace Tzkt.Sync.Services
{
    public class ObserverService(
        TezosNode _node,
        IServiceScopeFactory _services,
        IConfiguration _config,
        ILogger<ObserverService> _logger,
        IMetrics _metrics) : IHostedService
    {
        #region static
        const int SyncStatusTtl = 5;
        #endregion

        readonly CancellationTokenSource _cts = new();
        readonly HeadNotifier _headNotifier = HeadNotifier.Create(_config.GetObserverConfig(), _node, _logger);
        readonly Lock _lock = new();

        Task? _headNotifierTask;
        volatile Task? _updateTask;
        volatile Task? _applyTask;

        AppState _appState = null!;
        Header _head = Header.Empty();
        DateTime _syncedAt = DateTime.MinValue;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                await ResetState(cancellationToken);
                _logger.LogInformation("State initialized: [{level}:{hash}]", _appState.Level, _appState.Hash);

                await InitQuotes();
                _logger.LogInformation("Quotes initialized: [{level}]", _appState.QuoteLevel);

                _headNotifier.OnHead += OnHead;
                _headNotifierTask = _headNotifier.RunAsync(_cts.Token);

                _logger.LogInformation("Synchronization started");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
            catch (Exception ex)
            {
                // should never happen
                _logger.LogCritical(ex, "Observer crashed when starting");
                throw;
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                _headNotifier.OnHead -= OnHead;

                _cts.Cancel();
                if (_headNotifierTask != null) await _headNotifierTask;
                if (_updateTask != null) await _updateTask;
                if (_applyTask != null) await _applyTask;
                _cts.Dispose();

                _logger.LogInformation("Synchronization stopped");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
            catch (Exception ex)
            {
                // should never happen
                _logger.LogCritical(ex, "Observer crashed when stopping");
                throw;
            }
        }

        private void OnHead(Header head)
        {
            if (head.Hash != _head.Hash)
                _logger.LogDebug("New head [{level}:{hash}]", head.Level, head.Hash);

            lock (_lock)
            {
                _head = head;
                _syncedAt = DateTime.UtcNow;

                if (CanUpdateSyncStatus())
                    _updateTask ??= Task.Run(UpdateSyncStatus);

                if (CanApplyUpdates())
                    _applyTask ??= Task.Run(ApplyUpdates);
            }
        }

        private async Task UpdateSyncStatus()
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    _appState.KnownHead = _head.Level;
                    _appState.LastSync = _syncedAt.TrimMilliseconds();

                    _metrics.Measure.Gauge.SetHealthValue(_appState);

                    using var scope = _services.CreateScope();
                    using var db = scope.ServiceProvider.GetRequiredService<TzktContext>();

                    await db.Database.ExecuteSqlRawAsync("""
                        UPDATE "AppState"
                        SET "KnownHead" = {0},
                            "LastSync" = {1}
                        """, [_appState.KnownHead, _appState.LastSync], _cts.Token);

                    _logger.LogDebug("Sync status updated");
                }
                catch (OperationCanceledException) when (_cts.IsCancellationRequested) { }
                catch (Exception ex)
                {
                    // no big deal
                    _logger.LogError(ex, "Failed to update sync status");
                }

                lock (_lock)
                {
                    if (CanUpdateSyncStatus())
                        continue;

                    _updateTask = null;
                    return;
                }
            }
        }

        private async Task ApplyUpdates()
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    if (_appState.Level == _head.Level)
                    {
                        _logger.LogWarning("Chain reorg detected. Rebase local branch...");
                        await RebaseLocalBranch(_cts.Token);
                    }

                    await AdvanceLocalBranch(_cts.Token);

                    _logger.LogDebug("Updates applied");
                }
                catch (OperationCanceledException) when (_cts.IsCancellationRequested) { }
                catch (Exception ex)
                {
                    // should never happen
                    _logger.LogCritical(ex, "Failed to apply updates");
                }

                lock (_lock)
                {
                    if (CanApplyUpdates())
                        continue;

                    _applyTask = null;
                    return;
                }
            }
        }

        private async Task AdvanceLocalBranch(CancellationToken cancellationToken)
        {
            while (_appState.Level < _head.Level && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogDebug("Applying block...");
                    using (_metrics.Measure.Timer.Time(MetricsRegistry.ApplyBlockTime))
                    {
                        using var scope = _services.CreateScope();
                        var protocol = scope.ServiceProvider.GetProtocolHandler(_appState.Level + 1, _appState.NextProtocol);
                        _appState = await protocol.CommitNextBlock();
                    }
                    _metrics.Measure.Gauge.SetHealthValue(_appState);
                    _logger.LogInformation("Applied {level} of {total}", _appState.Level, _appState.KnownHead);
                }
                catch (BaseException ex) when (ex.RebaseRequired)
                {
                    _logger.LogWarning(ex, "Failed to apply block: rebase required. Rebase local branch...");
                    await ResetState(cancellationToken);
                    await RebaseLocalBranch(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to apply block. Retry in 5 sec...");
                    await Task.Delay(5000, cancellationToken);
                    await ResetState(cancellationToken);
                }
            }
        }

        private async Task RebaseLocalBranch(CancellationToken cancellationToken)
        {
            while (_appState.Level >= 0 && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var remote = await _node.GetAsync<Header>($"chains/main/blocks/{_appState.Level}/header");
                    if (_appState.Hash == remote.Hash) return;

                    _logger.LogDebug("Reverting block...");
                    using (_metrics.Measure.Timer.Time(MetricsRegistry.RevertBlockTime))
                    {
                        using var scope = _services.CreateScope();
                        var protocol = scope.ServiceProvider.GetProtocolHandler(_appState.Level, _appState.Protocol);
                        _appState = await protocol.RevertLastBlock(remote.Predecessor);
                    }
                    _metrics.Measure.Gauge.SetHealthValue(_appState);
                    _logger.LogInformation("Reverted to {level} of {total}", _appState.Level, _appState.KnownHead);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to revert block. Retry in 5 sec...");
                    await Task.Delay(5000, cancellationToken);
                    await ResetState(cancellationToken);
                }
            }
        }

        private async Task ResetState(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _services.CreateScope();
                    var cache = scope.ServiceProvider.GetRequiredService<CacheService>();

                    await cache.ResetAsync();

                    _appState = cache.AppState.Get();
                    _metrics.Measure.Gauge.SetHealthValue(_appState);

                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to reset state. Retry in 5 sec...");
                    await Task.Delay(5000, cancellationToken);
                }
            }
            cancellationToken.ThrowIfCancellationRequested();
        }

        private async Task InitQuotes()
        {
            using var scope = _services.CreateScope();
            var quotes = scope.ServiceProvider.GetRequiredService<QuotesService>();
            await quotes.Init();
        }

        private bool CanUpdateSyncStatus()
        {
            return _head.Level != _appState.KnownHead || _syncedAt >= _appState.LastSync.AddSeconds(SyncStatusTtl);
        }

        private bool CanApplyUpdates()
        {
            return _head.Level > _appState.Level || _head.Level == _appState.Level && _head.Hash != _appState.Hash;
        }
    }
}
