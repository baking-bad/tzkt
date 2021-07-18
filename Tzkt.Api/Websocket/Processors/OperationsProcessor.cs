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
    public class OperationsProcessor<T> : IHubProcessor where T : Hub
    {
        #region static
        const string OperationsGroup = "operations";
        const string OperationsChannel = "operations";
        static readonly SemaphoreSlim Sema = new(1, 1);

        static readonly Dictionary<Operations, Sub> TypesSubs = new();
        static readonly Dictionary<string, int> Limits = new();

        class Sub
        {
            public HashSet<string> All { get; set; }
            public Dictionary<string, HashSet<string>> Addresses { get; set; }

            public bool Empty => All == null && Addresses == null;
        }
        #endregion

        readonly StateCache State;
        readonly OperationRepository Repo;
        readonly IHubContext<T> Context;
        readonly WebsocketConfig Config;
        readonly ILogger Logger;

        public OperationsProcessor(StateCache state, OperationRepository repo, IHubContext<T> hubContext, IConfiguration config, ILogger<OperationsProcessor<T>> logger)
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

                #region check reorg
                if (State.Reorganized)
                {
                    Logger.LogDebug("Sending reorg message with state {0}", State.ValidLevel);
                    sendings.Add(Context.Clients
                        .Group(OperationsGroup)
                        .SendReorg(OperationsChannel, State.ValidLevel));
                }
                #endregion

                if (State.ValidLevel == State.Current.Level)
                {
                    Logger.LogDebug("No operations to send");
                    return;
                }

                #region load operations
                Logger.LogDebug("Fetching operations from block {0} to block {1}", State.ValidLevel, State.Current.Level);

                var level = new Int32Parameter
                {
                    Gt = State.ValidLevel,
                    Le = State.Current.Level
                };
                var limit = 1_000_000; // crutch
                var symbols = Symbols.None;

                var endorsements = TypesSubs.TryGetValue(Operations.Endorsements, out var endorsementsSub)
                    ? Repo.GetEndorsements(null, level, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.EndorsementOperation>());

                var proposals = TypesSubs.TryGetValue(Operations.Proposals, out var proposalsSub)
                    ? Repo.GetProposals(null, level, null, null, null, null, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.ProposalOperation>());

                var ballots = TypesSubs.TryGetValue(Operations.Ballots, out var ballotsSub)
                    ? Repo.GetBallots(null, level, null, null, null, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.BallotOperation>());

                var activations = TypesSubs.TryGetValue(Operations.Activations, out var activationsSub)
                    ? Repo.GetActivations(null, level, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.ActivationOperation>());

                var doubleBaking = TypesSubs.TryGetValue(Operations.DoubleBakings, out var doubleBakingSub)
                    ? Repo.GetDoubleBakings(null, null, null, level, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.DoubleBakingOperation>());

                var doubleEndorsing = TypesSubs.TryGetValue(Operations.DoubleEndorsings, out var doubleEndorsingSub)
                    ? Repo.GetDoubleEndorsings(null, null, null, level, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.DoubleEndorsingOperation>());

                var revelations = TypesSubs.TryGetValue(Operations.Revelations, out var revelationsSub)
                    ? Repo.GetNonceRevelations(null, null, null, level, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.NonceRevelationOperation>());

                var delegations = TypesSubs.TryGetValue(Operations.Delegations, out var delegationsSub)
                    ? Repo.GetDelegations(null, null, null, null, null, level, null, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.DelegationOperation>());

                var originations = TypesSubs.TryGetValue(Operations.Originations, out var originationsSub)
                    ? Repo.GetOriginations(null, null, null, null, null, null, null, null, level, null, null, null, null, limit, MichelineFormat.Json, symbols, true, true)
                    : Task.FromResult(Enumerable.Empty<Models.OriginationOperation>());

                var transactions = TypesSubs.TryGetValue(Operations.Transactions, out var transactionsSub)
                    ? Repo.GetTransactions(null, null, null, null, null, level, null, null, null, null, null, null, null, null, limit, MichelineFormat.Json, symbols, true, true)
                    : Task.FromResult(Enumerable.Empty<Models.TransactionOperation>());

                var reveals = TypesSubs.TryGetValue(Operations.Reveals, out var revealsSub)
                    ? Repo.GetReveals(null, level, null, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.RevealOperation>());

                var migrations = TypesSubs.TryGetValue(Operations.Migrations, out var migrationsSub)
                    ? Repo.GetMigrations(null, null, null, level, null, null, null, limit, MichelineFormat.Json, symbols, true, true)
                    : Task.FromResult(Enumerable.Empty<Models.MigrationOperation>());

                var penalties = TypesSubs.TryGetValue(Operations.RevelationPenalty, out var penaltiesSub)
                    ? Repo.GetRevelationPenalties(null, level, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.RevelationPenaltyOperation>());

                var baking = TypesSubs.TryGetValue(Operations.Baking, out var bakingSub)
                    ? Repo.GetBakings(null, level, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.BakingOperation>());

                await Task.WhenAll(
                    endorsements,
                    proposals,
                    ballots,
                    activations,
                    doubleBaking,
                    doubleEndorsing,
                    revelations,
                    delegations,
                    originations,
                    transactions,
                    reveals,
                    migrations,
                    penalties,
                    baking);
                #endregion

                #region prepare to send
                var toSend = new Dictionary<string, List<Operation>>();
                
                void Add(HashSet<string> subs, Operation operation)
                {
                    foreach (var clientId in subs)
                    {
                        if (!toSend.TryGetValue(clientId, out var list))
                        {
                            list = new();
                            toSend.Add(clientId, list);
                        }
                        list.Add(operation);
                    }
                }

                void AddRange(HashSet<string> subs, IEnumerable<Operation> operations)
                {
                    foreach (var clientId in subs)
                    {
                        if (!toSend.TryGetValue(clientId, out var list))
                        {
                            list = new();
                            toSend.Add(clientId, list);
                        }
                        list.AddRange(operations);
                    }
                }

                if (endorsements.Result.Any())
                {
                    if (endorsementsSub.All != null)
                        AddRange(endorsementsSub.All, endorsements.Result);

                    if (endorsementsSub.Addresses != null)
                        foreach (var op in endorsements.Result)
                            if (endorsementsSub.Addresses.TryGetValue(op.Delegate.Address, out var delegateSubs))
                                Add(delegateSubs, op);
                }

                if (ballots.Result.Any())
                {
                    if (ballotsSub.All != null)
                        AddRange(ballotsSub.All, ballots.Result);

                    if (ballotsSub.Addresses != null)
                        foreach (var op in ballots.Result)
                            if (ballotsSub.Addresses.TryGetValue(op.Delegate.Address, out var delegateSubs))
                                Add(delegateSubs, op);
                }

                if (proposals.Result.Any())
                {
                    if (proposalsSub.All != null)
                        AddRange(proposalsSub.All, proposals.Result);

                    if (proposalsSub.Addresses != null)
                        foreach (var op in proposals.Result)
                            if (proposalsSub.Addresses.TryGetValue(op.Delegate.Address, out var delegateSubs))
                                Add(delegateSubs, op);
                }

                if (activations.Result.Any())
                {
                    if (activationsSub.All != null)
                        AddRange(activationsSub.All, activations.Result);

                    if (activationsSub.Addresses != null)
                        foreach (var op in activations.Result)
                            if (activationsSub.Addresses.TryGetValue(op.Account.Address, out var accountSubs))
                                Add(accountSubs, op);
                }

                if (doubleBaking.Result.Any())
                {
                    if (doubleBakingSub.All != null)
                        AddRange(doubleBakingSub.All, doubleBaking.Result);

                    if (doubleBakingSub.Addresses != null)
                        foreach (var op in doubleBaking.Result)
                        {
                            if (doubleBakingSub.Addresses.TryGetValue(op.Accuser.Address, out var accuserSubs))
                                Add(accuserSubs, op);

                            if (doubleBakingSub.Addresses.TryGetValue(op.Offender.Address, out var offenderSubs))
                                Add(offenderSubs, op);
                        }
                }

                if (doubleEndorsing.Result.Any())
                {
                    if (doubleEndorsingSub.All != null)
                        AddRange(doubleEndorsingSub.All, doubleEndorsing.Result);

                    if (doubleEndorsingSub.Addresses != null)
                        foreach (var op in doubleEndorsing.Result)
                        {
                            if (doubleEndorsingSub.Addresses.TryGetValue(op.Accuser.Address, out var accuserSubs))
                                Add(accuserSubs, op);

                            if (doubleEndorsingSub.Addresses.TryGetValue(op.Offender.Address, out var offenderSubs))
                                Add(offenderSubs, op);
                        }
                }

                if (revelations.Result.Any())
                {
                    if (revelationsSub.All != null)
                        AddRange(revelationsSub.All, revelations.Result);

                    if (revelationsSub.Addresses != null)
                        foreach (var op in revelations.Result)
                        {
                            if (revelationsSub.Addresses.TryGetValue(op.Baker.Address, out var bakerSubs))
                                Add(bakerSubs, op);

                            if (revelationsSub.Addresses.TryGetValue(op.Sender.Address, out var senderSubs))
                                Add(senderSubs, op);
                        }
                }

                if (delegations.Result.Any())
                {
                    if (delegationsSub.All != null)
                        AddRange(delegationsSub.All, delegations.Result);

                    if (delegationsSub.Addresses != null)
                        foreach (var op in delegations.Result)
                        {
                            if (op.Initiator != null && delegationsSub.Addresses.TryGetValue(op.Initiator.Address, out var initiatorSubs))
                                Add(initiatorSubs, op);

                            if (delegationsSub.Addresses.TryGetValue(op.Sender.Address, out var senderSubs))
                                Add(senderSubs, op);

                            if (op.PrevDelegate != null && delegationsSub.Addresses.TryGetValue(op.PrevDelegate.Address, out var prevSubs))
                                Add(prevSubs, op);

                            if (op.NewDelegate != null && delegationsSub.Addresses.TryGetValue(op.NewDelegate.Address, out var newSubs))
                                Add(newSubs, op);
                        }
                }

                if (originations.Result.Any())
                {
                    if (originationsSub.All != null)
                        AddRange(originationsSub.All, originations.Result);

                    if (originationsSub.Addresses != null)
                        foreach (var op in originations.Result)
                        {
                            if (op.Initiator != null && originationsSub.Addresses.TryGetValue(op.Initiator.Address, out var initiatorSubs))
                                Add(initiatorSubs, op);

                            if (originationsSub.Addresses.TryGetValue(op.Sender.Address, out var senderSubs))
                                Add(senderSubs, op);

                            if (op.ContractManager != null && originationsSub.Addresses.TryGetValue(op.ContractManager.Address, out var managerSubs))
                                Add(managerSubs, op);

                            if (op.ContractDelegate != null && originationsSub.Addresses.TryGetValue(op.ContractDelegate.Address, out var delegateSubs))
                                Add(delegateSubs, op);
                        }
                }

                if (transactions.Result.Any())
                {
                    if (transactionsSub.All != null)
                        AddRange(transactionsSub.All, transactions.Result);

                    if (transactionsSub.Addresses != null)
                        foreach (var op in transactions.Result)
                        {
                            if (op.Initiator != null && transactionsSub.Addresses.TryGetValue(op.Initiator.Address, out var initiatorSubs))
                                Add(initiatorSubs, op);

                            if (transactionsSub.Addresses.TryGetValue(op.Sender.Address, out var senderSubs))
                                Add(senderSubs, op);

                            if (op.Target != null && transactionsSub.Addresses.TryGetValue(op.Target.Address, out var targetSubs))
                                Add(targetSubs, op);
                        }
                }

                if (reveals.Result.Any())
                {
                    if (revealsSub.All != null)
                        AddRange(revealsSub.All, reveals.Result);

                    if (revealsSub.Addresses != null)
                        foreach (var op in reveals.Result)
                            if (revealsSub.Addresses.TryGetValue(op.Sender.Address, out var senderSubs))
                                Add(senderSubs, op);
                }

                if (migrations.Result.Any())
                {
                    if (migrationsSub.All != null)
                        AddRange(migrationsSub.All, migrations.Result);

                    if (migrationsSub.Addresses != null)
                        foreach (var op in migrations.Result)
                            if (migrationsSub.Addresses.TryGetValue(op.Account.Address, out var accountSubs))
                                Add(accountSubs, op);
                }

                if (penalties.Result.Any())
                {
                    if (penaltiesSub.All != null)
                        AddRange(penaltiesSub.All, penalties.Result);

                    if (penaltiesSub.Addresses != null)
                        foreach (var op in penalties.Result)
                            if (penaltiesSub.Addresses.TryGetValue(op.Baker.Address, out var bakerSubs))
                                Add(bakerSubs, op);
                }

                if (baking.Result.Any())
                {
                    if (bakingSub.All != null)
                        AddRange(bakingSub.All, baking.Result);

                    if (bakingSub.Addresses != null)
                        foreach (var op in baking.Result)
                            if (bakingSub.Addresses.TryGetValue(op.Baker.Address, out var bakerSubs))
                                Add(bakerSubs, op);
                }
                #endregion

                #region send
                foreach (var (connectionId, operations) in toSend.Where(x => x.Value.Count > 0))
                {
                    var data = operations.Count > 1
                        ? Distinct(operations).OrderBy(x => x.Id)
                        : (IEnumerable<Operation>)operations;

                    sendings.Add(Context.Clients
                        .Client(connectionId)
                        .SendData(OperationsChannel, data, State.Current.Level));

                    Logger.LogDebug("{0} operations sent to {1}", operations.Count, connectionId);
                }
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
                    Logger.LogError("Sendings failed: {0}", ex.Message);
                }
                #endregion
            }
        }

        public async Task Subscribe(IClientProxy client, string connectionId, OperationsParameter parameter)
        {
            Task sending = Task.CompletedTask;
            try
            {
                await Sema.WaitAsync();
                Logger.LogDebug("New subscription...");

                #region check limits
                if (Limits.TryGetValue(connectionId, out var cnt) && cnt >= Config.MaxOperationSubscriptions)
                    throw new HubException($"Subscriptions limit exceeded");

                if (cnt > 0) // reuse already allocated string
                    connectionId = Limits.Keys.First(x => x == connectionId);
                #endregion

                #region add to subs
                foreach (var type in parameter.TypesList)
                {
                    if (!TypesSubs.TryGetValue(type, out var typeSub))
                    {
                        typeSub = new();
                        TypesSubs.Add(type, typeSub);
                    }
                    if (parameter.Address == null)
                    {
                        typeSub.All ??= new();
                        if (typeSub.All.Add(connectionId))
                            Limits[connectionId] = Limits.GetValueOrDefault(connectionId) + 1;
                    }   
                    else
                    {
                        typeSub.Addresses ??= new(4);
                        if (!typeSub.Addresses.TryGetValue(parameter.Address, out var addressSub))
                        {
                            addressSub = new(4);
                            typeSub.Addresses.Add(parameter.Address, addressSub);
                        }

                        if (addressSub.Add(connectionId))
                            Limits[connectionId] = Limits.GetValueOrDefault(connectionId) + 1;
                    }
                }
                #endregion

                #region add to group
                await Context.Groups.AddToGroupAsync(connectionId, OperationsGroup);
                #endregion

                sending = client.SendState(OperationsChannel, State.Current.Level);

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
                    Logger.LogError("Sending failed: {0}", ex.Message);
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

                foreach (var (type, typeSub) in TypesSubs)
                {
                    if (typeSub.All != null)
                    {
                        if (typeSub.All.Remove(connectionId))
                            Limits[connectionId]--;

                        if (typeSub.All.Count == 0)
                            typeSub.All = null;
                    }

                    if (typeSub.Addresses != null)
                    {
                        foreach (var (address, addressSubs) in typeSub.Addresses)
                        {
                            if (addressSubs.Remove(connectionId))
                                Limits[connectionId]--;

                            if (addressSubs.Count == 0)
                                typeSub.Addresses.Remove(address);
                        }

                        if (typeSub.Addresses.Count == 0)
                            typeSub.Addresses = null;
                    }

                    if (typeSub.Empty)
                        TypesSubs.Remove(type);
                }

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

        private static IEnumerable<Operation> Distinct(List<Operation> ops)
        {
            var set = new HashSet<int>(ops.Count);
            foreach (var op in ops)
                if (set.Add(op.Id))
                    yield return op;
        }
    }
}