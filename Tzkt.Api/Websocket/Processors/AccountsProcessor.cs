using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Tzkt.Api.Models;
using Tzkt.Api.Repositories;
using Tzkt.Api.Services.Cache;
using Tzkt.Data.Models;
using Account = Tzkt.Api.Models.Account;

namespace Tzkt.Api.Websocket.Processors
{
    public class AccountsProcessor<T> : IHubProcessor where T : Hub
    {
        #region static
        const string AccountsGroup = "accounts";
        const string AccountsChannel = "accounts";
        static readonly SemaphoreSlim Sema = new(1, 1);

        static readonly Dictionary<string, ContractSub> AccountSubs = new();

        static readonly Dictionary<string, int> Limits = new();

        //TODO To HashSet of string
        class ContractSub
        {
            public HashSet<string> All { get; set; }

            public bool Empty => All == null;
        }
        #endregion

        readonly StateCache State;
        readonly AccountRepository Repo;
        readonly IHubContext<T> Context;
        readonly WebsocketConfig Config;
        readonly ILogger Logger;

        public AccountsProcessor(StateCache state, AccountRepository accounts, IHubContext<T> hubContext, IConfiguration config, ILogger<AccountsProcessor<T>> logger)
        {
            State = state;
            Repo = accounts;
            Context = hubContext;
            Config = config.GetWebsocketConfig();
            Logger = logger;
        }
        
        public async Task OnStateChanged()
        {
            var sendings = new List<Task>(Limits.Count);
            try
            {
                await Sema.WaitAsync();

                if (Limits.Count == 0)
                {
                    Logger.LogDebug("No account subs");
                    return;
                }

                #region check reorg
                if (State.Reorganized)
                {
                    Logger.LogDebug("Sending reorg message with state {0}", State.ValidLevel);
                    sendings.Add(Context.Clients
                        .Group(AccountsGroup)
                        .SendReorg(AccountsChannel, State.ValidLevel));
                }
                #endregion

                if (State.ValidLevel == State.Current.Level)
                {
                    Logger.LogDebug("No accounts to send");
                    return;
                }

                #region load updates
                Logger.LogDebug("Fetching account updates from {0} to {1}", State.ValidLevel, State.Current.Level);

                var level = new Int32Parameter
                {
                    Gt = State.ValidLevel,
                    Le = State.Current.Level
                };
                const int limit = 1_000_000;

                var updates = (await Repo.Get(null, null, null, null, null, level, null, null, limit)).ToList();
                var count = updates.Count;

                Logger.LogDebug("{0} account updates fetched", count);
                #endregion

                #region prepare to send
                var toSend = new Dictionary<string, List<Account>>();

                //TODO WAAT?
                void Add(HashSet<string> subs, Account update)
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
                    if (AccountSubs.TryGetValue(update.Address, out var accountSubs))
                    {
                        if (accountSubs.All != null)
                            Add(accountSubs.All, update);
                    }
                }
                #endregion

                #region send
                foreach (var (connectionId, updatesList) in toSend.Where(x => x.Value.Count > 0))
                {
                    var data = updatesList.Count > 1
                        ? Distinct(updatesList).OrderBy(x => x.Address)
                        : (IEnumerable<Account>)updatesList;

                    sendings.Add(Context.Clients
                        .Client(connectionId)
                        .SendData(AccountsChannel, data, State.Current.Level));

                    Logger.LogDebug("{0} account updates sent to {1}", updatesList.Count, connectionId);
                }

                Logger.LogDebug("{0} account updates sent", count);
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

        public async Task Subscribe(IClientProxy client, string connectionId, AccountsParameter parameter)
        {
            Task sending = Task.CompletedTask;
            try
            {
                await Sema.WaitAsync();
                Logger.LogDebug("New subscription...");

                #region check limits
                if (Limits.TryGetValue(connectionId, out var cnt) && cnt >= Config.MaxAccountSubscriptions)
                    throw new HubException($"Subscriptions limit exceeded");
                
                if (cnt > 0) // reuse already allocated string
                    connectionId = Limits.Keys.First(x => x == connectionId);
                #endregion

                #region add to subs
                if (parameter.Address != null)
                {
                    if (!AccountSubs.TryGetValue(parameter.Address, out var accountSub))
                    {
                        accountSub = new();
                        AccountSubs.Add(parameter.Address, accountSub);
                    }
                    accountSub.All ??= new(4);
                    TryAdd(accountSub.All, connectionId);
                }
                else
                {
                    throw new HubException("Empty address parameter");
                }
                #endregion

                await Context.Groups.AddToGroupAsync(connectionId, AccountsGroup);

                sending = client.SendState(AccountsChannel, State.Current.Level);

                Logger.LogDebug("Client {0} subscribed with state {1}", connectionId, State.Current.Level);
            }
            catch (HubException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to add subscription: {0}", ex.Message);
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

        public async Task Unsubscribe(string connectionId)
        {
            try
            {
                await Sema.WaitAsync();
                if (!Limits.ContainsKey(connectionId)) return;
                Logger.LogDebug("Remove subscription...");

                foreach (var accountSub in AccountSubs.Values)
                {
                    if (accountSub.All != null)
                    {
                        TryRemove(accountSub.All, connectionId);
                        if (accountSub.All.Count == 0)
                            accountSub.All = null;
                    }
                }

                foreach (var account in AccountSubs.Where(x => x.Value.Empty).Select(x => x.Key).ToList())
                    AccountSubs.Remove(account);

                if (Limits[connectionId] != 0)
                    Logger.LogCritical("Failed to unsibscribe {0}: {1} subs left", connectionId, Limits[connectionId]);
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

        private static void TryAdd(HashSet<string> set, string connectionId)
        {
            if (!set.Contains(connectionId))
            {
                set.Add(connectionId);
                Limits[connectionId] = Limits.GetValueOrDefault(connectionId) + 1;
            }
        }

        private static void TryRemove(HashSet<string> set, string connectionId)
        {
            if (set.Remove(connectionId))
                Limits[connectionId]--;
        }

        private static IEnumerable<Account> Distinct(List<Account> items)
        {
            var hashset = new HashSet<string>(items.Count);
            foreach (var item in items)
            {
                if (!hashset.Contains(item.Address))
                {
                    hashset.Add(item.Address);
                    yield return item;
                }
            }
        }
    }
}
