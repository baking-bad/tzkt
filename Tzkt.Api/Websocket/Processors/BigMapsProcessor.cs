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
using Tzkt.Data.Models;

namespace Tzkt.Api.Websocket.Processors
{
    public class BigMapsProcessor<T> : IHubProcessor where T : Hub
    {
        #region static
        const string BigMapsGroup = "bigmaps";
        const string BigMapsChannel = "bigmaps";
        static readonly SemaphoreSlim Sema = new(1, 1);

        static readonly HashSet<string> AllSubs = new();
        static readonly Dictionary<int, HashSet<string>> PtrSubs = new();
        static readonly Dictionary<BigMapTag, HashSet<string>> TagSubs = new();
        static readonly Dictionary<string, ContractSub> ContractSubs = new();

        static readonly Dictionary<string, int> Limits = new();

        class ContractSub
        {
            public HashSet<string> All { get; set; }
            public Dictionary<string, HashSet<string>> Paths { get; set; }
            public Dictionary<BigMapTag, HashSet<string>> Tags { get; set; }

            public bool Empty => All == null && Paths == null && Tags == null;
        }
        #endregion

        readonly StateCache State;
        readonly BigMapsRepository Repo;
        readonly IHubContext<T> Context;
        readonly WebsocketConfig Config;
        readonly ILogger Logger;

        public BigMapsProcessor(StateCache state, BigMapsRepository bigMaps, IHubContext<T> hubContext, IConfiguration config, ILogger<BigMapsProcessor<T>> logger)
        {
            State = state;
            Repo = bigMaps;
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
                    Logger.LogDebug("No bigmap subs");
                    return;
                }

                #region check reorg
                if (State.Reorganized)
                {
                    Logger.LogDebug("Sending reorg message with state {state}", State.ValidLevel);
                    sendings.Add(Context.Clients
                        .Group(BigMapsGroup)
                        .SendReorg(BigMapsChannel, State.ValidLevel));
                }
                #endregion

                if (State.ValidLevel == State.Current.Level)
                {
                    Logger.LogDebug("No bigmaps to send");
                    return;
                }

                #region load updates
                Logger.LogDebug("Fetching bigmap updates from {valid} to {current}", State.ValidLevel, State.Current.Level);

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
                var format = MichelineFormat.Json;

                var updates = await Repo.GetUpdates(null, null, null, level, null, null, null, limit, format);
                var count = updates.Count();

                Logger.LogDebug("{cnt} bigmap updates fetched", count);
                #endregion

                #region prepare to send
                var toSend = new Dictionary<string, List<Models.BigMapUpdate>>();

                void Add(HashSet<string> subs, Models.BigMapUpdate update)
                {
                    foreach (var clientId in subs)
                    {
                        if (!toSend.TryGetValue(clientId, out var list))
                        {
                            list = new(4);
                            toSend.Add(clientId, list);
                        }
                        list.Add(update);
                    }
                }

                foreach (var update in updates)
                {
                    #region all subs
                    Add(AllSubs, update);
                    #endregion

                    #region ptr subs
                    if (PtrSubs.TryGetValue(update.Bigmap, out var ptrSubs))
                        Add(ptrSubs, update);
                    #endregion

                    #region tag subs
                    foreach (var tag in update.EnumerateTags())
                        if (TagSubs.TryGetValue(tag, out var tagSubs))
                            Add(tagSubs, update);
                    #endregion

                    #region contract subs
                    if (ContractSubs.TryGetValue(update.Contract.Address, out var contractSubs))
                    {
                        if (contractSubs.All != null)
                            Add(contractSubs.All, update);

                        if (contractSubs.Paths != null)
                            if (contractSubs.Paths.TryGetValue(update.Path, out var contractPathSubs))
                                Add(contractPathSubs, update);

                        if (contractSubs.Tags != null)
                            foreach (var tag in update.EnumerateTags())
                                if (contractSubs.Tags.TryGetValue(tag, out var contractTagSubs))
                                    Add(contractTagSubs, update);
                    }
                    #endregion
                }
                #endregion

                #region send
                foreach (var (connectionId, updatesList) in toSend.Where(x => x.Value.Count > 0))
                {
                    var data = updatesList.Count > 1
                        ? Distinct(updatesList).OrderBy(x => x.Id)
                        : (IEnumerable<Models.BigMapUpdate>)updatesList;

                    sendings.Add(Context.Clients
                        .Client(connectionId)
                        .SendData(BigMapsChannel, data, State.Current.Level));

                    Logger.LogDebug("{cnt} bigmap updates sent to {id}", updatesList.Count, connectionId);
                }

                Logger.LogDebug("{cnt} bigmap updates sent", count);
                #endregion
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to process state change");
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
                    Logger.LogCritical(ex, "Sendings failed");
                }
                #endregion
            }
        }

        public async Task<int> Subscribe(IClientProxy client, string connectionId, BigMapsParameter parameter)
        {
            Task sending = Task.CompletedTask;
            try
            {
                await Sema.WaitAsync();
                Logger.LogDebug("New subscription...");

                #region check limits
                if (Limits.TryGetValue(connectionId, out var cnt) && cnt >= Config.MaxBigMapSubscriptions)
                    throw new HubException($"Subscriptions limit exceeded");
                
                if (cnt > 0) // reuse already allocated string
                    connectionId = Limits.Keys.First(x => x == connectionId);
                #endregion

                #region add to subs
                if (parameter.Ptr != null)
                {
                    TryAdd(PtrSubs, (int)parameter.Ptr, connectionId);
                }
                else if (parameter.Contract != null)
                {
                    if (!ContractSubs.TryGetValue(parameter.Contract, out var contractSub))
                    {
                        contractSub = new();
                        ContractSubs.Add(parameter.Contract, contractSub);
                    }
                    if (parameter.Path != null)
                    {
                        contractSub.Paths ??= new(4);
                        TryAdd(contractSub.Paths, parameter.Path, connectionId);
                    }
                    else if (parameter.TagsList != null)
                    {
                        contractSub.Tags ??= new(4);
                        foreach (var tag in parameter.TagsList)
                            TryAdd(contractSub.Tags, tag, connectionId);
                    }
                    else
                    {
                        contractSub.All ??= new(4);
                        TryAdd(contractSub.All, connectionId);
                    }
                }
                else if (parameter.TagsList != null)
                {
                    foreach (var tag in parameter.TagsList)
                        TryAdd(TagSubs, tag, connectionId);
                }
                else
                {
                    TryAdd(AllSubs, connectionId);
                }
                #endregion

                #region add to group
                await Context.Groups.AddToGroupAsync(connectionId, BigMapsGroup);
                #endregion

                sending = client.SendState(BigMapsChannel, State.Current.Level);

                Logger.LogDebug("Client {id} subscribed with state {state}", connectionId, State.Current.Level);
                return State.Current.Level;
            }
            catch (HubException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to add subscription");
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
                    Logger.LogCritical(ex, "Sending failed");
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
                TryRemove(PtrSubs, connectionId);
                TryRemove(TagSubs, connectionId);
                
                foreach (var contractSub in ContractSubs.Values)
                {
                    if (contractSub.All != null)
                    {
                        TryRemove(contractSub.All, connectionId);
                        if (contractSub.All.Count == 0)
                            contractSub.All = null;
                    }
                    if (contractSub.Paths != null)
                    {
                        TryRemove(contractSub.Paths, connectionId);
                        if (contractSub.Paths.Count == 0)
                            contractSub.Paths = null;
                    }
                    if (contractSub.Tags != null)
                    {
                        TryRemove(contractSub.Tags, connectionId);
                        if (contractSub.Tags.Count == 0)
                            contractSub.Tags = null;
                    }
                }

                foreach (var contract in ContractSubs.Where(x => x.Value.Empty).Select(x => x.Key).ToList())
                    ContractSubs.Remove(contract);

                if (Limits[connectionId] != 0)
                    Logger.LogCritical("Failed to unsubscribe {id}: {cnt} subs left", connectionId, Limits[connectionId]);
                Limits.Remove(connectionId);

                Logger.LogDebug("Client {id} unsubscribed", connectionId);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to remove subscription");
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

        private static IEnumerable<Models.BigMapUpdate> Distinct(List<Models.BigMapUpdate> items)
        {
            var set = new HashSet<int>(items.Count);
            foreach (var item in items)
                if (set.Add(item.Id))
                    yield return item;
        }
    }
}
