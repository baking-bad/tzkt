﻿using System.Numerics;
using Microsoft.AspNetCore.SignalR;
using Tzkt.Api.Repositories;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Websocket.Processors
{
    public class TokenBalancesProcessor<T> : IHubProcessor where T : Hub
    {
        #region static
        const string Group = "token_balances";
        const string Channel = "token_balances";
        static readonly SemaphoreSlim Sema = new(1, 1);

        static readonly HashSet<string> AllSubs = [];
        static readonly Dictionary<string, AccountSub> AccountSubs = [];
        static readonly Dictionary<string, ContractSub> ContractSubs = [];

        static readonly Dictionary<string, int> Limits = [];

        class AccountSub
        {
            public HashSet<string>? All { get; set; }
            public Dictionary<string, ContractSub>? Contracts { get; set; }

            public bool Empty => All == null && Contracts == null;
        }
        class ContractSub
        {
            public HashSet<string>? All { get; set; }
            public Dictionary<BigInteger, HashSet<string>>? Tokens { get; set; }

            public bool Empty => All == null && Tokens == null;
        }
        #endregion

        readonly StateCache State;
        readonly TokensRepository Repo;
        readonly IHubContext<T> Context;
        readonly WebsocketConfig Config;
        readonly ILogger Logger;

        public TokenBalancesProcessor(StateCache state, TokensRepository tokens,
            IHubContext<T> hubContext, IConfiguration config, ILogger<TokenBalancesProcessor<T>> logger)
        {
            State = state;
            Repo = tokens;
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
                    Logger.LogDebug("No token balances subs");
                    return;
                }

                #region check reorg
                if (State.Reorganized)
                {
                    Logger.LogDebug("Sending reorg message with state {state}", State.ValidLevel);
                    sendings.Add(Context.Clients
                        .Group(Group)
                        .SendReorg(Channel, State.ValidLevel));
                }
                #endregion

                if (State.ValidLevel == State.Current.Level)
                {
                    Logger.LogDebug("No token balances to send");
                    return;
                }

                #region load token balances
                Logger.LogDebug("Fetching token balances from {valid} to {current}", State.ValidLevel, State.Current.Level);

                var params1 = new TokenBalanceFilter
                {
                    lastLevel = State.Current.Level == State.ValidLevel + 1
                        ? new Int32Parameter
                        {
                            Eq = State.Current.Level
                        }
                        : new Int32Parameter
                        {
                            Gt = State.ValidLevel,
                            Le = State.Current.Level
                        }
                };
                var params2 = new TokenBalanceFilter
                {
                    lastLevel = new Int32Parameter
                    {
                        Le = State.ValidLevel
                    },
                    indexedAt = State.Current.Level == State.ValidLevel + 1
                        ? new Int32NullParameter
                        {
                            Null = false,
                            Eq = State.Current.Level
                        }
                        : new Int32NullParameter
                        {
                            Null = false,
                            Gt = State.ValidLevel,
                            Le = State.Current.Level
                        }
                };
                var limit = 1_000_000;

                var balances = (await Repo.GetTokenBalances(params1, new() { limit = limit }))
                    // we do the second requests because there may be new token balances added retroactively
                    .Concat(await Repo.GetTokenBalances(params2, new() { limit = limit }));
                var count = balances.Count();

                Logger.LogDebug("{cnt} token balances fetched", count);
                #endregion

                #region prepare to send
                var toSend = new Dictionary<string, List<Models.TokenBalance>>();

                void Add(HashSet<string> subs, Models.TokenBalance balance)
                {
                    foreach (var clientId in subs)
                    {
                        if (!toSend.TryGetValue(clientId, out var list))
                        {
                            list = [];
                            toSend.Add(clientId, list);
                        }
                        list.Add(balance);
                    }
                }

                foreach (var balance in balances)
                {
                    #region all subs
                    Add(AllSubs, balance);
                    #endregion

                    #region account subs
                    if (AccountSubs.TryGetValue(balance.Account.Address, out var accountSubs))
                    {
                        if (accountSubs.All != null)
                            Add(accountSubs.All, balance);

                        if (accountSubs.Contracts != null)
                        {
                            if (accountSubs.Contracts.TryGetValue(balance.Token.Contract.Address, out var accountContractSubs))
                            {
                                if (accountContractSubs.All != null)
                                    Add(accountContractSubs.All, balance);

                                if (accountContractSubs.Tokens != null)
                                    if (accountContractSubs.Tokens.TryGetValue(balance.Token.TokenId, out var contractTokenSubs))
                                        Add(contractTokenSubs, balance);
                            }
                        }
                    }
                    #endregion

                    #region contract subs
                    if (ContractSubs.TryGetValue(balance.Token.Contract.Address, out var contractSubs))
                    {
                        if (contractSubs.All != null)
                            Add(contractSubs.All, balance);

                        if (contractSubs.Tokens != null)
                            if (contractSubs.Tokens.TryGetValue(balance.Token.TokenId, out var contractTokenSubs))
                                Add(contractTokenSubs, balance);
                    }
                    #endregion
                }
                #endregion

                #region send
                foreach (var (connectionId, balancesList) in toSend.Where(x => x.Value.Count > 0))
                {
                    var data = balancesList.Count > 1
                        ? Distinct(balancesList).OrderBy(x => x.Id)
                        : (IEnumerable<Models.TokenBalance>)balancesList;

                    sendings.Add(Context.Clients
                        .Client(connectionId)
                        .SendData(Channel, data, State.Current.Level));

                    Logger.LogDebug("{cnt} token balances sent to {id}", balancesList.Count, connectionId);
                }

                Logger.LogDebug("{cnt} token balances sent", count);
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

        public async Task<int> Subscribe(IClientProxy client, string connectionId, TokenTransfersParameter parameter)
        {
            Task sending = Task.CompletedTask;
            try
            {
                await Sema.WaitAsync();
                Logger.LogDebug("New subscription...");

                #region check limits
                if (Limits.TryGetValue(connectionId, out var cnt) && cnt >= Config.MaxTokenBalancesSubscriptions)
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
                    if (parameter.Contract != null)
                    {
                        accountSub.Contracts ??= new(4);
                        if (!accountSub.Contracts.TryGetValue(parameter.Contract, out var contractSub))
                        {
                            contractSub = new();
                            accountSub.Contracts.Add(parameter.Contract, contractSub);
                        }
                        if (parameter.TokenId != null)
                        {
                            contractSub.Tokens ??= new(4);
                            TryAdd(contractSub.Tokens, parameter.TokenId.Value, connectionId);
                        }    
                        else
                        {
                            contractSub.All ??= new(4);
                            TryAdd(contractSub.All, connectionId);
                        }
                    }
                    else
                    {
                        accountSub.All ??= new(4);
                        TryAdd(accountSub.All, connectionId);
                    }
                }
                else if (parameter.Contract != null)
                {
                    if (!ContractSubs.TryGetValue(parameter.Contract, out var contractSub))
                    {
                        contractSub = new();
                        ContractSubs.Add(parameter.Contract, contractSub);
                    }
                    if (parameter.TokenId != null)
                    {
                        contractSub.Tokens ??= new(4);
                        TryAdd(contractSub.Tokens, parameter.TokenId.Value, connectionId);
                    }
                    else
                    {
                        contractSub.All ??= [];
                        TryAdd(contractSub.All, connectionId);
                    }
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
                    if (accountSub.Contracts != null)
                    {
                        var emptyAccountContractSubs = new List<string>();
                        foreach (var (contract, contractSub) in accountSub.Contracts)
                        {
                            contractSub.All = TryRemove(contractSub.All, connectionId);
                            contractSub.Tokens = TryRemove(contractSub.Tokens, connectionId);
                            
                            if (contractSub.Empty)
                                emptyAccountContractSubs.Add(contract);
                        }
                        foreach (var contract in emptyAccountContractSubs)
                            accountSub.Contracts.Remove(contract);

                        if (accountSub.Contracts.Count == 0)
                            accountSub.Contracts = null;
                    }
                    if (accountSub.Empty)
                        emptyAccountSubs.Add(account);
                }
                foreach (var account in emptyAccountSubs)
                    AccountSubs.Remove(account);
                #endregion

                #region contract subs
                var emptyContractSubs = new List<string>();
                foreach (var (contract, contractSub) in ContractSubs)
                {
                    contractSub.All = TryRemove(contractSub.All, connectionId);
                    contractSub.Tokens = TryRemove(contractSub.Tokens, connectionId);

                    if (contractSub.Empty)
                        emptyContractSubs.Add(contract);
                }
                foreach (var contract in emptyContractSubs)
                    ContractSubs.Remove(contract);
                #endregion

                if (Limits[connectionId] != 0)
                    Logger.LogCritical("Failed to unsibscribe {id}: {cnt} subs left", connectionId, Limits[connectionId]);
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

        private static void TryAdd<TSubKey>(Dictionary<TSubKey, HashSet<string>> subs, TSubKey key, string connectionId) where TSubKey : notnull
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

        private static Dictionary<TSubKey, HashSet<string>>? TryRemove<TSubKey>(Dictionary<TSubKey, HashSet<string>>? subs, string connectionId) where TSubKey : notnull
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

        private static HashSet<string>? TryRemove(HashSet<string>? set, string connectionId)
        {
            if (set == null) return null;
            if (set.Remove(connectionId))
            {
                Limits[connectionId]--;
                if (set.Count == 0) return null;
            }
            return set;
        }

        private static IEnumerable<Models.TokenBalance> Distinct(List<Models.TokenBalance> items)
        {
            var set = new HashSet<long>(items.Count);
            foreach (var item in items)
                if (set.Add(item.Id))
                    yield return item;
        }
    }
}
