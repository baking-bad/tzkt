using System;
using System.Collections.Generic;
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
        readonly string ConnectionString;

        readonly StateCache State;
        readonly AccountsCache Accounts;
        readonly ProtocolsCache Protocols;
        readonly QuotesCache Quotes;
        readonly TimeCache Times;
        readonly HomeService Home;
        readonly IEnumerable<IHubProcessor> Processors;
        readonly ILogger Logger;

        Task Notifying = Task.CompletedTask;
        readonly List<(int Level, string Hash)> Changes = new List<(int, string)>(4);

        public StateListener(
            StateCache state,
            AccountsCache accounts,
            ProtocolsCache protocols,
            QuotesCache quotes,
            TimeCache times,
            HomeService home,
            IEnumerable<IHubProcessor> processors,
            IConfiguration config,
            ILogger<StateListener> logger)
        {
            ConnectionString = config.GetConnectionString("DefaultConnection");

            State = state;
            Accounts = accounts;
            Protocols = protocols;
            Quotes = quotes;
            Times = times;
            Home = home;
            Processors = processors;
            Logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                Logger.LogInformation("State listener started");

                using var db = new NpgsqlConnection(ConnectionString);
                db.Notification += OnStateChanged;
                await db.OpenAsync(cancellationToken);
                await db.ExecuteAsync("LISTEN state_changed;");

                while (!cancellationToken.IsCancellationRequested)
                    await db.WaitAsync(cancellationToken);

                db.Notification -= OnStateChanged;
            }
            catch (Exception ex)
            {
                // TODO: reanimate listener without breaking HubProcessors state
                Logger.LogCritical($"State listener crashed: {ex.Message}");
            }
            finally
            {
                Logger.LogWarning("State listener stopped");
            }
        }

        private void OnStateChanged(object sender, NpgsqlNotificationEventArgs e)
        {
            Logger.LogDebug("Received {1} notification with payload {2}", e.Channel, e.Payload);

            if (e.Payload == null)
            {
                Logger.LogCritical("Invalid trigger payload");
                return;
            }

            var data = e.Payload.Split(':', StringSplitOptions.RemoveEmptyEntries);
            if (data.Length != 2 || !int.TryParse(data[0], out var level) || data[1].Length != 51)
            {
                Logger.LogCritical("Invalid trigger payload {1}", e.Payload);
                return;
            }

            lock (Changes)
            {
                Changes.Add((level, data[1]));

                if (Notifying.IsCompleted)
                    Notifying = NotifyAsync(); // async run
            }
        }

        private async Task NotifyAsync()
        {
            try
            {
                Logger.LogDebug("Processing notification...");

                #region update state
                RawState newState;
                List<(int, string)> changes;
                var attempts = 0;

                while (true)
                {
                    if (attempts++ > 32)
                    {
                        // should never get here, but to make sure there are no infinite loops...
                        Logger.LogCritical("Failed to reach state equal to trigger's payload '{1}'", Changes[^1].Hash);
                        return;
                    }

                    newState = await State.LoadAsync();
                    lock (Changes)
                    {
                        if (newState.Hash != Changes[^1].Hash)
                        {
                            Logger.LogDebug("Lost sync. Retrying...");
                            continue;
                        }

                        changes = Changes.ToList();
                        Changes.Clear();
                        break;
                    }
                }

                State.Update(newState, changes);
                #endregion

                #region update cache
                await Accounts.UpdateAsync();
                await Protocols.UpdateAsync();
                await Quotes.UpdateAsync();
                await Times.UpdateAsync();
                #endregion

                #region send events
                foreach (var processor in Processors)
                    _ = processor.OnStateChanged();
                #endregion

                #region update home
                _ = Home.UpdateAsync();
                #endregion

                Logger.LogDebug("Notification processed");

                lock (Changes)
                {
                    if (Changes.Count > 0)
                    {
                        Logger.LogDebug("Handle pending notification");
                        Notifying = NotifyAsync(); // async run
                    }
                    else
                    {
                        Notifying = Task.CompletedTask;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to process notification: {1}", ex.Message);
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
