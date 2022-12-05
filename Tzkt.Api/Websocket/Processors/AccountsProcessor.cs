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
    public class AccountsProcessor<T> : IHubProcessor where T : Hub
    {
        #region static
        const string AccountsGroup = "accounts";
        const string AccountsChannel = "accounts";
        static readonly SemaphoreSlim Sema = new(1, 1);

        static readonly HashSet<string> AllSubs = new();
        static readonly Dictionary<string, HashSet<string>> AccountSubs = new();

        static readonly Dictionary<string, int> Limits = new();
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
                    Logger.LogDebug("Sending reorg message with state {state}", State.ValidLevel);
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
                Logger.LogDebug("Fetching account updates from {valid} to {current}", State.ValidLevel, State.Current.Level);

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
                const int limit = 1_000_000;

                var accounts = (await Repo.Get(null, null, null, null, null, null, null, level, null, null, limit)).ToList();

                Logger.LogDebug("{cnt} account updates fetched", accounts.Count);
                #endregion

                #region prepare to send
                var toSend = new Dictionary<string, List<Account>>();

                foreach (var account in accounts)
                {
                    foreach (var clientId in AllSubs)
                    {
                        if (!toSend.TryGetValue(clientId, out var list))
                        {
                            list = new(4);
                            toSend.Add(clientId, list);
                        }
                        list.Add(account);
                    }

                    if (AccountSubs.TryGetValue(account.Address, out var accountSubs))
                    {
                        foreach (var clientId in accountSubs)
                        {
                            if (!toSend.TryGetValue(clientId, out var list))
                            {
                                list = new(4);
                                toSend.Add(clientId, list);
                            }
                            list.Add(account);
                        }
                    }
                }
                #endregion

                #region send
                foreach (var (connectionId, updatesList) in toSend.Where(x => x.Value.Count > 0))
                {
                    // TODO: distinct by Id
                    sendings.Add(Context.Clients
                        .Client(connectionId)
                        .SendData(AccountsChannel, updatesList, State.Current.Level));

                    Logger.LogDebug("{cnt} account updates sent to {id}", updatesList.Count, connectionId);
                }

                Logger.LogDebug("{cnt} account updates sent", accounts.Count);
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

        public async Task<int> Subscribe(IClientProxy client, string connectionId, AccountsParameter parameter)
        {
            Task sending = Task.CompletedTask;
            try
            {
                await Sema.WaitAsync();
                Logger.LogDebug("New subscription...");

                #region check limits
                var cnt = Limits.GetValueOrDefault(connectionId);

                if (cnt + (parameter.Addresses?.Count ?? 0) > Config.MaxAccountsSubscriptions)
                    throw new HubException($"Subscriptions limit exceeded");
                
                if (cnt > 0) // reuse already allocated string
                    connectionId = Limits.Keys.First(x => x == connectionId);
                #endregion

                #region add to subs
                if (parameter.Addresses?.Count > 0)
                {
                    foreach (var address in parameter.Addresses)
                    {
                        if (!AccountSubs.TryGetValue(address, out var accountSub))
                        {
                            accountSub = new(4);
                            AccountSubs.Add(address, accountSub);
                        }

                        if (accountSub.Add(connectionId))
                            Limits[connectionId] = Limits.GetValueOrDefault(connectionId) + 1;
                    }
                }
                else
                {
                    if (AllSubs.Add(connectionId))
                        Limits[connectionId] = Limits.GetValueOrDefault(connectionId) + 1;
                }
                #endregion

                await Context.Groups.AddToGroupAsync(connectionId, AccountsGroup);

                sending = client.SendState(AccountsChannel, State.Current.Level);

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

                if (AllSubs.Remove(connectionId))
                    Limits[connectionId]--;

                foreach (var (key, value) in AccountSubs)
                {
                    if (value.Remove(connectionId))
                        Limits[connectionId]--;
                    
                    if (value.Count == 0)
                        AccountSubs.Remove(key);
                }

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
    }
}
