using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

using Tzkt.Api.Repositories;
using Tzkt.Api.Services.Cache;
using Tzkt.Api.Models;

namespace Tzkt.Api.Websocket.Processors
{
    public class CyclesProcessor<T> : IHubProcessor where T : Hub
    {
        #region static
        const string CycleGroup = "cycles";
        const string CycleChannel = "cycles";
        static readonly SemaphoreSlim Sema = new(1, 1);
        static readonly Dictionary<int, HashSet<string>> DelaySubs = new();
        HashSet<string> Subs = new();
        #endregion

        readonly StateCache StateCache;
        readonly CyclesRepository CyclesRepo;
        readonly IHubContext<T> Context;
        readonly ILogger Logger;

        Cycle CurrentCycle = null;

        public CyclesProcessor(StateCache cache, CyclesRepository repo, IHubContext<T> hubContext, ILogger<CyclesProcessor<T>> logger)
        {
            StateCache = cache;
            CyclesRepo = repo;
            Context = hubContext;
            Logger = logger;
        }

        public async Task OnStateChanged()
        {
            var sendings = new List<Task>(2);
            try
            {
                await Sema.WaitAsync();
                if (Subs.Count == 0)
                {
                    Logger.LogDebug("No cycles subs");
                    return;
                }

                if (CurrentCycle == null || StateCache.Current.Level < CurrentCycle.FirstLevel || StateCache.Current.Level > CurrentCycle.LastLevel)
                {
                    CurrentCycle = await CyclesRepo.Get(StateCache.Current.Cycle, Symbols.None);
                }

                // we notify only group of clients with matching delay
                if (DelaySubs.TryGetValue(StateCache.Current.Level - CurrentCycle.FirstLevel, out var connections))
                {
                    foreach (var connectionId in connections)
                    {
                        sendings.Add(Context.Clients
                           .Client(connectionId)
                           .SendData(CycleChannel, CurrentCycle, StateCache.Current.Cycle));
                    }
                    Logger.LogDebug("Cycle {0} sent", StateCache.Current.Cycle);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to process state change: {0}", ex.Message);
            }
            finally
            {
                Sema.Release();
                #region await sendings
                try
                {
                    await Task.WhenAll(sendings);
                }
                catch (Exception ex)
                {
                    // should never get here
                    Logger.LogError("Sendings failed: {0}", ex.Message);
                }
                #endregion
            }
        }

        public async Task<int> Subscribe(IClientProxy client, string connectionId, CyclesParameter parameter)
        {
            Task sending = Task.CompletedTask;
            try
            {
                await Sema.WaitAsync();
                if (Subs.Contains(connectionId))
                {
                    throw new HubException($"{connectionId} already subscribed.");
                }

                Logger.LogDebug("New subscription...");

                if (!DelaySubs.TryGetValue(parameter.DelayBlocks, out var delaySub))
                {
                    delaySub = new(4);
                    DelaySubs.Add(parameter.DelayBlocks, delaySub);
                }
                Subs.Add(connectionId);
                delaySub.Add(connectionId);

                await Context.Groups.AddToGroupAsync(connectionId, CycleGroup);
                sending = client.SendState(CycleChannel, StateCache.Current.Cycle);

                Logger.LogDebug("Client {0} subscribed with state {1}", connectionId, StateCache.Current.Cycle);
                return StateCache.Current.Level;
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to add subscription: {0}", ex.Message);
                return 0;
            }
            finally
            {
                Sema.Release();
                try
                {
                    await sending;
                }
                catch (Exception ex)
                {
                    // should never get here
                    Logger.LogError("Sending failed: {0}", ex.Message);
                }
            }
        }

        public void Unsubscribe(string connectionId)
        {
            try
            {
                Sema.Wait();
                Logger.LogDebug("Remove subscription...");

                foreach (var (key, value) in DelaySubs)
                {
                    value.Remove(connectionId);
                    if (value.Count == 0)
                        DelaySubs.Remove(key);
                    Subs.Remove(connectionId);
                }

                Logger.LogDebug("Client {0} unsubscribed", connectionId);
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to remove subscription: {0}", ex.Message);
            }
            finally
            {
                Sema.Release();
            }
        }
    }
}
