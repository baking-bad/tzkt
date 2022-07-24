﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

using Tzkt.Api.Repositories;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Websocket.Processors
{
    public class CycleProcessor<T> : IHubProcessor where T : Hub
    {
        #region static
        const string CycleGroup = "cycle";
        const string CycleChannel = "cycle";
        static readonly SemaphoreSlim Sema = new(1, 1);
        static readonly Dictionary<int, HashSet<string>> DelaySubs = new();
        #endregion

        readonly StateCache StateCache;
        readonly StateRepository StateRepo;
        readonly IHubContext<T> Context;
        readonly ILogger Logger;

        private int cycleStartLevel = 0;
        private int lastProcessedCycle = 0;
        
        public CycleProcessor(StateCache cache, StateRepository repo, IHubContext<T> hubContext, ILogger<CycleProcessor<T>> logger)
        {
            StateCache = cache;
            StateRepo = repo;
            Context = hubContext;
            Logger = logger;
        }

        public async Task OnStateChanged()
        {
            var sendings = new List<Task>(2);
            try
            {
                await Sema.WaitAsync();
              
                if (lastProcessedCycle != StateCache.Current.Cycle)
                    cycleStartLevel = StateCache.Current.Level;
                lastProcessedCycle = StateCache.Current.Cycle;

                // we notify only group of clients with matching delay
                if (DelaySubs.TryGetValue(StateCache.Current.Level - cycleStartLevel, out var connections))
                {
                    foreach (var connectionId in connections)
                    {
                        sendings.Add(Context.Clients
                           .Client(connectionId)
                           .SendData(CycleChannel, StateRepo.Get(), StateCache.Current.Cycle));
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

        public async Task<int> Subscribe(IClientProxy client, string connectionId, CycleParameter parameter)
        {
            Task sending = Task.CompletedTask;
            try
            {
                await Sema.WaitAsync();
                Logger.LogDebug("New subscription...");

                if (!DelaySubs.TryGetValue(parameter.DelayBlocks, out var delaySub))
                {
                    delaySub = new(4);
                    DelaySubs.Add(parameter.DelayBlocks, delaySub);
                }
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