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
using Dapper;
using Npgsql;
using Tzkt.Api.Services.Cache;
using Tzkt.Api.Websocket;

namespace Tzkt.Api.Services.Sync
{
    public class StateListener : BackgroundService
    {
        #region static
        const string SyncStateChanged = "sync_state_changed";
        const string StateHashChanged = "state_hash_changed";
        const string StateMetadataChanged = "state_metadata_changed";
        const string AccountMetadataChanged = "account_metadata_changed";
        const string ProposalMetadataChanged = "proposal_metadata_changed";
        const string ProtocolMetadataChanged = "protocol_metadata_changed";
        const string SoftwareMetadataChanged = "software_metadata_changed";
        const string ConstantMetadataChanged = "constant_metadata_changed";
        const string BlockMetadataChanged = "block_metadata_changed";
        #endregion

        readonly string ConnectionString;

        readonly StateCache State;
        readonly BigMapsCache BigMaps;
        readonly AccountsCache Accounts;
        readonly AliasesCache Aliases;
        readonly ProtocolsCache Protocols;
        readonly SoftwareCache Software;
        readonly QuotesCache Quotes;
        readonly TimeCache Times;
        readonly ResponseCacheService OutputCache;
        readonly HomeService Home;
        readonly IEnumerable<IHubProcessor> Processors;
        readonly ILogger Logger;

        Task StateNotifying = Task.CompletedTask;
        readonly List<(int Level, string Hash)> StateChanges = new(4);

        public StateListener(
            StateCache state,
            BigMapsCache bigMaps,
            AccountsCache accounts,
            AliasesCache aliases,
            SoftwareCache software,
            ProtocolsCache protocols,
            QuotesCache quotes,
            TimeCache times,
            ResponseCacheService outputCache,
            HomeService home,
            IEnumerable<IHubProcessor> processors,
            IConfiguration config,
            ILogger<StateListener> logger)
        {
            ConnectionString = config.GetConnectionString("DefaultConnection");

            State = state;
            BigMaps = bigMaps;
            Accounts = accounts;
            Aliases = aliases;
            Protocols = protocols;
            Software = software;
            Quotes = quotes;
            Times = times;
            OutputCache = outputCache;
            Home = home;
            Processors = processors;
            Logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                Logger.LogInformation("DB listener started");

                using var db = new NpgsqlConnection(ConnectionString);
                db.Notification += OnNotification;

                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        if (db.State != ConnectionState.Open)
                        {
                            await db.OpenAsync(cancellationToken);
                            await db.ExecuteAsync($@"
                                LISTEN {SyncStateChanged};
                                LISTEN {StateHashChanged};
                                LISTEN {AccountMetadataChanged};
                                LISTEN {SoftwareMetadataChanged};");
                                //LISTEN {ConstantMetadataChanged};
                                //LISTEN {StateMetadataChanged};
                                //LISTEN {ProposalMetadataChanged};
                                //LISTEN {ProtocolMetadataChanged};
                                //LISTEN {BlockMetadataChanged};
                            Logger.LogInformation("Db listener connected");
                        }
                        await db.WaitAsync(cancellationToken);
                    }
                    catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested) { break; }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { break; }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "DB listener disconnected");
                        await Task.Delay(1000, cancellationToken);
                    }
                }

                db.Notification -= OnNotification;
            }
            catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested) { }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
            catch (Exception ex)
            {
                Logger.LogCritical(ex, "DB listener crashed");
            }
            finally
            {
                Logger.LogWarning("DB listener stopped");
            }
        }

        private void OnNotification(object sender, NpgsqlNotificationEventArgs e)
        {
            Logger.LogDebug("Received {channel} notification with payload {payload}", e.Channel, e.Payload);

            if (e.Payload == null)
            {
                Logger.LogCritical("Invalid trigger payload");
                return;
            }

            if (e.Channel == SyncStateChanged)
            {
                var separator = e.Payload.IndexOf(':');
                if (separator == -1 ||
                    !int.TryParse(e.Payload[..separator], out var knownHead) ||
                    !DateTimeOffset.TryParse(e.Payload[(separator + 1)..], out var lastSync))
                {
                    Logger.LogCritical("Invalid trigger payload {payload}", e.Payload);
                    return;
                }
                State.UpdateSyncState(knownHead, lastSync.UtcDateTime);
            }
            else if (e.Channel == StateHashChanged)
            {
                var data = e.Payload.Split(':', StringSplitOptions.RemoveEmptyEntries);
                if (data.Length != 2 || !int.TryParse(data[0], out var level) || data[1].Length != 51)
                {
                    Logger.LogCritical("Invalid trigger payload {payload}", e.Payload);
                    return;
                }

                lock (StateChanges)
                {
                    StateChanges.Add((level, data[1]));

                    if (StateNotifying.IsCompleted)
                        StateNotifying = NotifyStateAsync(); // async run
                }
            }
            else
            {
                NotifyMetadata(e.Channel, e.Payload);
            }
        }

        async Task NotifyStateAsync()
        {
            try
            {
                Logger.LogDebug("Processing state notification...");

                #region update state
                RawState newState;
                List<(int, string)> changes;
                var attempts = 0;

                while (true)
                {
                    if (attempts++ > 32)
                    {
                        // should never get here, but to make sure there are no infinite loops...
                        Logger.LogCritical("Failed to reach state equal to trigger's payload '{hash}'", StateChanges[^1].Hash);
                        return;
                    }

                    newState = await State.LoadAsync();
                    lock (StateChanges)
                    {
                        if (newState.Hash != StateChanges[^1].Hash)
                        {
                            Logger.LogDebug("Lost sync. Retrying...");
                            continue;
                        }

                        changes = StateChanges.ToList();
                        StateChanges.Clear();
                        break;
                    }
                }

                OutputCache.Clear();
                State.Update(newState, changes);
                #endregion

                #region update cache
                await Accounts.UpdateAsync();
                await BigMaps.UpdateAsync();
                await Protocols.UpdateAsync();
                await Quotes.UpdateAsync();
                await Times.UpdateAsync();
                OutputCache.Clear();
                #endregion

                #region send events
                foreach (var processor in Processors)
                    _ = processor.OnStateChanged();
                #endregion

                #region update home
                _ = Home.UpdateAsync();
                #endregion

                Logger.LogDebug("State notification processed");

                lock (StateChanges)
                {
                    if (StateChanges.Count > 0)
                    {
                        Logger.LogDebug("Handle pending state notification");
                        StateNotifying = NotifyStateAsync(); // async run
                    }
                    else
                    {
                        StateNotifying = Task.CompletedTask;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to process state notification");
            }
        }

        void NotifyMetadata(string channel, string payload)
        {
            try
            {
                Logger.LogDebug("Processing metadata notification...");

                var divider = payload.IndexOf(':');
                if (divider == -1)
                {
                    Logger.LogError("Invalid metadata notification payload");
                    return;
                }

                var key = payload[0..divider];
                var value = payload[(divider + 1)..];
                if (value == string.Empty) value = null;

                switch (channel)
                {
                    //case StateMetadataChanged:
                    //    break;
                    case AccountMetadataChanged:
                        Accounts.UpdateMetadata(key, value);
                        Aliases.UpdateMetadata(key, value);
                        break;
                    //case ProposalMetadataChanged:
                    //    break;
                    //case ProtocolMetadataChanged:
                    //    break;
                    case SoftwareMetadataChanged:
                        Software.UpdateMetadata(key);
                        break;
                    //case ConstantMetadataChanged:
                    //    break;
                    //case BlockMetadataChanged:
                    //    break;
                    default:
                        break;
                }

                Logger.LogDebug("Metadata notification processed");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to process metadata notification");
            }
        }
    }

    public static class StateListenerExt
    {
        public static void AddStateListener(this IServiceCollection services)
        {
            services.AddHostedService<StateListener>();
        }
    }
}
