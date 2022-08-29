using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Tzkt.Api.Models;
using Tzkt.Api.Repositories;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Websocket.Processors
{
    public class EventsProcessor<T> : IHubProcessor where T : Hub
    {
        #region static
        const string Group = "events";
        const string Channel = "events";
        static readonly SemaphoreSlim Sema = new(1, 1);

        static readonly HashSet<string> AllSubs = new();
        static readonly Dictionary<string, HashSet<string>> TagSubs = new();
        static readonly Dictionary<string, Sub> ContractSubs = new();
        static readonly Dictionary<int, Sub> CodeHashSubs = new();

        static readonly Dictionary<string, int> Limits = new();

        class Sub
        {
            public HashSet<string> All { get; set; }
            public Dictionary<string, HashSet<string>> Tags { get; set; }

            public bool Empty => All == null && Tags == null;
        }
        #endregion

        readonly StateCache State;
        readonly ContractEventsRepository Repo;
        readonly IHubContext<T> Context;
        readonly WebsocketConfig Config;
        readonly ILogger Logger;

        public EventsProcessor(StateCache state, ContractEventsRepository repo, IHubContext<T> hubContext, IConfiguration config, ILogger<EventsProcessor<T>> logger)
        {
            State = state;
            Repo = repo;
            Context = hubContext;
            Config = config.GetWebsocketConfig();
            Logger = logger;
        }
        
        public async Task OnStateChanged()
        {
            var sendings = new List<Task>();
            try
            {
                await Sema.WaitAsync();

                if (Limits.Count == 0)
                {
                    Logger.LogDebug("No event subs");
                    return;
                }

                #region check reorg
                if (State.Reorganized)
                {
                    Logger.LogDebug("Sending reorg message with state {0}", State.ValidLevel);
                    sendings.Add(Context.Clients
                        .Group(Group)
                        .SendReorg(Channel, State.ValidLevel));
                }
                #endregion

                if (State.ValidLevel == State.Current.Level)
                {
                    Logger.LogDebug("No events to send");
                    return;
                }

                #region load updates
                Logger.LogDebug("Fetching events from {0} to {1}", State.ValidLevel, State.Current.Level);

                var level = State.Current.Level == State.ValidLevel + 1
                    ? new Int32Parameter
                    {
                        Eq = State.Current.Level
                    }
                    : new Int32Parameter
                    {
                        Gt = State.ValidLevel,
                        Le = State.Current.Level
                    };
                var limit = 1_000_000;

                var events = await Repo.GetContractEvents(new() { level = level }, new() { limit = limit });
                var count = events.Count();

                Logger.LogDebug("{0} events fetched", count);
                #endregion

                #region prepare to send
                var toSend = new Dictionary<string, List<ContractEvent>>();

                void Add(HashSet<string> subs, ContractEvent e)
                {
                    foreach (var clientId in subs)
                    {
                        if (!toSend.TryGetValue(clientId, out var list))
                        {
                            list = new(4);
                            toSend.Add(clientId, list);
                        }
                        list.Add(e);
                    }
                }

                foreach (var e in events)
                {
                    #region all subs
                    Add(AllSubs, e);
                    #endregion

                    #region tag subs
                    if (TagSubs.TryGetValue(e.Tag, out var tagSubs))
                        Add(tagSubs, e);
                    #endregion

                    #region contract subs
                    if (ContractSubs.TryGetValue(e.Contract.Address, out var contractSubs))
                    {
                        if (contractSubs.All != null)
                            Add(contractSubs.All, e);

                        if (contractSubs.Tags != null)
                            if (contractSubs.Tags.TryGetValue(e.Tag, out var contractTagSubs))
                                Add(contractTagSubs, e);
                    }
                    #endregion

                    #region codehash subs
                    if (CodeHashSubs.TryGetValue(e.CodeHash, out var codeHashSubs))
                    {
                        if (codeHashSubs.All != null)
                            Add(codeHashSubs.All, e);

                        if (codeHashSubs.Tags != null)
                            if (codeHashSubs.Tags.TryGetValue(e.Tag, out var codeHashTagSubs))
                                Add(codeHashTagSubs, e);
                    }
                    #endregion
                }
                #endregion

                #region send
                foreach (var (connectionId, eventsList) in toSend.Where(x => x.Value.Count > 0))
                {
                    var data = eventsList.Count > 1
                        ? Distinct(eventsList).OrderBy(x => x.Id)
                        : (IEnumerable<ContractEvent>)eventsList;

                    sendings.Add(Context.Clients
                        .Client(connectionId)
                        .SendData(Channel, data, State.Current.Level));

                    Logger.LogDebug("{0} events sent to {1}", eventsList.Count, connectionId);
                }

                Logger.LogDebug("{0} events sent", count);
                #endregion
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
                    Logger.LogCritical("Sendings failed: {0}", ex.Message);
                }
                #endregion
            }
        }

        public async Task<int> Subscribe(IClientProxy client, string connectionId, EventsParameter parameter)
        {
            Task sending = Task.CompletedTask;
            try
            {
                await Sema.WaitAsync();
                Logger.LogDebug("New subscription...");

                #region check limits
                if (Limits.TryGetValue(connectionId, out var cnt) && cnt >= Config.MaxEventSubscriptions)
                    throw new HubException($"Subscriptions limit exceeded");
                
                if (cnt > 0) // reuse already allocated string
                    connectionId = Limits.Keys.First(x => x == connectionId);
                #endregion

                #region add to subs
                if (parameter.Contract != null)
                {
                    if (!ContractSubs.TryGetValue(parameter.Contract, out var contractSub))
                    {
                        contractSub = new();
                        ContractSubs.Add(parameter.Contract, contractSub);
                    }
                    if (parameter.Tag != null)
                    {
                        contractSub.Tags ??= new(4);
                        TryAdd(contractSub.Tags, parameter.Tag, connectionId);
                    }
                    else
                    {
                        contractSub.All ??= new();
                        TryAdd(contractSub.All, connectionId);
                    }
                }
                else if (parameter.CodeHash is int codeHash)
                {
                    if (!CodeHashSubs.TryGetValue(codeHash, out var codeHashSub))
                    {
                        codeHashSub = new();
                        CodeHashSubs.Add(codeHash, codeHashSub);
                    }
                    if (parameter.Tag != null)
                    {
                        codeHashSub.Tags ??= new(4);
                        TryAdd(codeHashSub.Tags, parameter.Tag, connectionId);
                    }
                    else
                    {
                        codeHashSub.All ??= new();
                        TryAdd(codeHashSub.All, connectionId);
                    }
                }
                else if (parameter.Tag != null)
                {
                    TryAdd(TagSubs, parameter.Tag, connectionId);
                }
                else
                {
                    TryAdd(AllSubs, connectionId);
                }
                #endregion

                #region add to group
                await Context.Groups.AddToGroupAsync(connectionId, Group);
                #endregion

                sending = client.SendState(Channel, State.Current.Level);

                Logger.LogDebug("Client {0} subscribed with state {1}", connectionId, State.Current.Level);
                return State.Current.Level;
            }
            catch (HubException)
            {
                throw;
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
                    Logger.LogCritical("Sending failed: {0}", ex.Message);
                }
            }
        }

        public void Unsubscribe(string connectionId)
        {
            try
            {
                Sema.Wait();
                if (!Limits.ContainsKey(connectionId)) return;
                Logger.LogDebug("Remove subscription...");

                TryRemove(AllSubs, connectionId);
                TryRemove(TagSubs, connectionId);
                
                foreach (var (key, sub) in ContractSubs)
                {
                    if (sub.All != null)
                    {
                        TryRemove(sub.All, connectionId);
                        if (sub.All.Count == 0)
                            sub.All = null;
                    }
                    if (sub.Tags != null)
                    {
                        TryRemove(sub.Tags, connectionId);
                        if (sub.Tags.Count == 0)
                            sub.Tags = null;
                    }
                    if (sub.Empty)
                        ContractSubs.Remove(key);
                }

                foreach (var (key, sub) in CodeHashSubs)
                {
                    if (sub.All != null)
                    {
                        TryRemove(sub.All, connectionId);
                        if (sub.All.Count == 0)
                            sub.All = null;
                    }
                    if (sub.Tags != null)
                    {
                        TryRemove(sub.Tags, connectionId);
                        if (sub.Tags.Count == 0)
                            sub.Tags = null;
                    }
                    if (sub.Empty)
                        CodeHashSubs.Remove(key);
                }

                if (Limits[connectionId] != 0)
                    Logger.LogCritical("Failed to unsubscribe {0}: {1} subs left", connectionId, Limits[connectionId]);
                Limits.Remove(connectionId);

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

        private static void TryAdd<TSubKey>(Dictionary<TSubKey, HashSet<string>> subs, TSubKey key, string connectionId)
        {
            if (!subs.TryGetValue(key, out var set))
            {
                set = new(4);
                subs.Add(key, set);
            }

            if (set.Add(connectionId))
                Limits[connectionId] = Limits.GetValueOrDefault(connectionId) + 1;
        }

        private static void TryAdd(HashSet<string> set, string connectionId)
        {
            if (set.Add(connectionId))
                Limits[connectionId] = Limits.GetValueOrDefault(connectionId) + 1;
        }

        private static void TryRemove<TSubKey>(Dictionary<TSubKey, HashSet<string>> subs, string connectionId)
        {
            foreach (var (key, value) in subs)
            {
                if (value.Remove(connectionId))
                    Limits[connectionId]--;

                if (value.Count == 0)
                    subs.Remove(key);
            }
        }

        private static void TryRemove(HashSet<string> set, string connectionId)
        {
            if (set.Remove(connectionId))
                Limits[connectionId]--;
        }

        private static IEnumerable<Models.ContractEvent> Distinct(List<Models.ContractEvent> items)
        {
            var set = new HashSet<int>(items.Count);
            foreach (var item in items)
                if (set.Add(item.Id))
                    yield return item;
        }
    }
}
