using Microsoft.AspNetCore.SignalR;
using Tzkt.Api.Models;
using Tzkt.Api.Repositories;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Websocket.Processors
{
    public class TicketTransfersProcessor<T> : IHubProcessor where T : Hub
    {
        #region static
        const string TicketTransfersGroup = "ticket_transfers";
        const string TicketTransfersChannel = "ticket_transfers";
        static readonly SemaphoreSlim Sema = new(1, 1);

        static readonly HashSet<string> AllSubs = new();
        static readonly Dictionary<string, AccountSub> AccountSubs = new();
        static readonly Dictionary<string, TicketerSub> TicketerSubs = new();

        static readonly Dictionary<string, int> Limits = new();

        class AccountSub
        {
            public HashSet<string> All { get; set; }
            public Dictionary<string, TicketerSub> Ticketers { get; set; }

            public bool Empty => All == null && Ticketers == null;
        }
        class TicketerSub
        {
            public HashSet<string> All { get; set; }

            public bool Empty => All == null;
        }
        #endregion

        readonly StateCache State;
        readonly TicketsRepository Repo;
        readonly IHubContext<T> Context;
        readonly WebsocketConfig Config;
        readonly ILogger Logger;

        public TicketTransfersProcessor(StateCache state, TicketsRepository tickets,
            IHubContext<T> hubContext, IConfiguration config, ILogger<TicketTransfersProcessor<T>> logger)
        {
            State = state;
            Repo = tickets;
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
                    Logger.LogDebug("No ticket transfers subs");
                    return;
                }

                #region check reorg
                if (State.Reorganized)
                {
                    Logger.LogDebug("Sending reorg message with state {state}", State.ValidLevel);
                    sendings.Add(Context.Clients
                        .Group(TicketTransfersGroup)
                        .SendReorg(TicketTransfersChannel, State.ValidLevel));
                }
                #endregion

                if (State.ValidLevel == State.Current.Level)
                {
                    Logger.LogDebug("No ticket transfers to send");
                    return;
                }

                #region load ticket transfers
                Logger.LogDebug("Fetching ticket transfers from block {valid} to block {current}", State.ValidLevel, State.Current.Level);

                var params1 = new TicketTransferFilter
                {
                    level = State.Current.Level == State.ValidLevel + 1
                        ? new Int32Parameter
                        {
                            Eq = State.Current.Level
                        }
                        : new Int32Parameter
                        {
                            Gt = State.ValidLevel,
                            Le = State.Current.Level
                        },
                    ticket = new()
                };
                var limit = 1_000_000;
                var format = MichelineFormat.Json;


                var transfers = (await Repo.GetTicketTransfers(params1, new() { limit = limit }, format));
                var count = transfers.Count();

                Logger.LogDebug("{cnt} ticket transfers fetched", count);
                #endregion

                #region prepare to send
                var toSend = new Dictionary<string, List<TicketTransfer>>();

                void Add(HashSet<string> subs, TicketTransfer transfer)
                {
                    foreach (var clientId in subs)
                    {
                        if (!toSend.TryGetValue(clientId, out var list))
                        {
                            list = new();
                            toSend.Add(clientId, list);
                        }
                        list.Add(transfer);
                    }
                }

                foreach (var transfer in transfers)
                {
                    #region all subs
                    Add(AllSubs, transfer);
                    #endregion

                    #region account subs
                    if (transfer.From != null)
                    {
                        if (AccountSubs.TryGetValue(transfer.From.Address, out var accountSubs))
                        {
                            if (accountSubs.All != null)
                                Add(accountSubs.All, transfer);

                            if (accountSubs.Ticketers != null)
                            {
                                if (accountSubs.Ticketers.TryGetValue(transfer.Ticket.Ticketer.Address, out var accountTicketerSubs))
                                {
                                    if (accountTicketerSubs.All != null)
                                        Add(accountTicketerSubs.All, transfer);
                                }
                            }
                        }
                    }
                    if (transfer.To != null)
                    {
                        if (AccountSubs.TryGetValue(transfer.To.Address, out var accountSubs))
                        {
                            if (accountSubs.All != null)
                                Add(accountSubs.All, transfer);

                            if (accountSubs.Ticketers != null)
                            {
                                if (accountSubs.Ticketers.TryGetValue(transfer.Ticket.Ticketer.Address, out var accountTicketerSubs))
                                {
                                    if (accountTicketerSubs.All != null)
                                        Add(accountTicketerSubs.All, transfer);
                                }
                            }
                        }
                    }
                    #endregion

                    #region ticketer subs
                    if (TicketerSubs.TryGetValue(transfer.Ticket.Ticketer.Address, out var ticketerSubs))
                    {
                        if (ticketerSubs.All != null)
                            Add(ticketerSubs.All, transfer);
                    }
                    #endregion
                }
                #endregion

                #region send
                foreach (var (connectionId, transfersList) in toSend.Where(x => x.Value.Count > 0))
                {
                    var data = transfersList.Count > 1
                        ? Distinct(transfersList).OrderBy(x => x.Id)
                        : (IEnumerable<TicketTransfer>)transfersList;

                    sendings.Add(Context.Clients
                        .Client(connectionId)
                        .SendData(TicketTransfersChannel, data, State.Current.Level));

                    Logger.LogDebug("{cnt} ticket transfers sent to {id}", transfersList.Count, connectionId);
                }

                Logger.LogDebug("{cnt} ticket transfers sent", count);
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

        public async Task<int> Subscribe(IClientProxy client, string connectionId, TicketTransfersParameter parameter)
        {
            Task sending = Task.CompletedTask;
            try
            {
                await Sema.WaitAsync();
                Logger.LogDebug("New subscription...");

                #region check limits
                if (Limits.TryGetValue(connectionId, out var cnt) && cnt >= Config.MaxTicketTransfersSubscriptions)
                    throw new HubException($"Subscriptions limit exceeded");
                
                if (cnt > 0) // reuse already allocated string
                    connectionId = Limits.Keys.First(x => x == connectionId);
                #endregion

                #region add to subs
                if (parameter.Account != null)
                {
                    if (!AccountSubs.TryGetValue(parameter.Account, out var accountSub))
                    {
                        accountSub = new();
                        AccountSubs.Add(parameter.Account, accountSub);
                    }
                    if (parameter.Ticketer != null)
                    {
                        accountSub.Ticketers ??= new(4);
                        if (!accountSub.Ticketers.TryGetValue(parameter.Ticketer, out var ticketerSub))
                        {
                            ticketerSub = new();
                            accountSub.Ticketers.Add(parameter.Ticketer, ticketerSub);
                        }   
                        else
                        {
                            ticketerSub.All ??= new(4);
                            TryAdd(ticketerSub.All, connectionId);
                        }
                    }
                    else
                    {
                        accountSub.All ??= new(4);
                        TryAdd(accountSub.All, connectionId);
                    }
                }
                else if (parameter.Ticketer != null)
                {
                    if (!TicketerSubs.TryGetValue(parameter.Ticketer, out var ticketerSub))
                    {
                        ticketerSub = new();
                        TicketerSubs.Add(parameter.Ticketer, ticketerSub);
                    }
                    else
                    {
                        ticketerSub.All ??= new();
                        TryAdd(ticketerSub.All, connectionId);
                    }
                }
                else
                {
                    TryAdd(AllSubs, connectionId);
                }
                #endregion

                #region add to group
                await Context.Groups.AddToGroupAsync(connectionId, TicketTransfersGroup);
                #endregion

                sending = client.SendState(TicketTransfersChannel, State.Current.Level);

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

                #region all subs
                TryRemove(AllSubs, connectionId);
                #endregion

                #region account subs
                var emptyAccountSubs = new List<string>();
                foreach (var (account, accountSub) in AccountSubs)
                {
                    accountSub.All = TryRemove(accountSub.All, connectionId);
                    if (accountSub.Ticketers != null)
                    {
                        var emptyAccountTicketerSubs = new List<string>();
                        foreach (var (ticketer, ticketerSub) in accountSub.Ticketers)
                        {
                            ticketerSub.All = TryRemove(ticketerSub.All, connectionId);
                            
                            if (ticketerSub.Empty)
                                emptyAccountTicketerSubs.Add(ticketer);
                        }
                        foreach (var ticketer in emptyAccountTicketerSubs)
                            accountSub.Ticketers.Remove(ticketer);

                        if (accountSub.Ticketers.Count == 0)
                            accountSub.Ticketers = null;
                    }
                    if (accountSub.Empty)
                        emptyAccountSubs.Add(account);
                }
                foreach (var account in emptyAccountSubs)
                    AccountSubs.Remove(account);
                #endregion

                #region ticketer subs
                var emptyTicketerSubs = new List<string>();
                foreach (var (ticketer, ticketerSub) in TicketerSubs)
                {
                    ticketerSub.All = TryRemove(ticketerSub.All, connectionId);

                    if (ticketerSub.Empty)
                        emptyTicketerSubs.Add(ticketer);
                }
                foreach (var ticketer in emptyTicketerSubs)
                    TicketerSubs.Remove(ticketer);
                #endregion

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

        private static Dictionary<TSubKey, HashSet<string>> TryRemove<TSubKey>(Dictionary<TSubKey, HashSet<string>> subs, string connectionId)
        {
            if (subs == null) return null;
            foreach (var (key, value) in subs)
            {
                if (value.Remove(connectionId))
                    Limits[connectionId]--;

                if (value.Count == 0)
                    subs.Remove(key);
            }
            if (subs.Count == 0) return null;
            return subs;
        }

        private static HashSet<string> TryRemove(HashSet<string> set, string connectionId)
        {
            if (set == null) return null;
            if (set.Remove(connectionId))
            {
                Limits[connectionId]--;
                if (set.Count == 0) return null;
            }
            return set;
        }

        private static IEnumerable<TicketTransfer> Distinct(List<TicketTransfer> items)
        {
            var set = new HashSet<long>(items.Count);
            foreach (var item in items)
                if (set.Add(item.Id))
                    yield return item;
        }
    }
}
