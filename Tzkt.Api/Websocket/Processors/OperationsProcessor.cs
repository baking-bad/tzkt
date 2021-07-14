using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
        static readonly SemaphoreSlim Sema = new SemaphoreSlim(1, 1);

        static readonly Dictionary<string, Operations> Subs = new();
        static readonly Dictionary<string, Dictionary<string, Operations>> AddressSubs = new();
        static Operations ActiveOps = Operations.None;
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
            var sendings = new List<Task>(AddressSubs.Count + Subs.Count);
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

                var endorsements = ActiveOps.HasFlag(Operations.Endorsements)
                    ? Repo.GetEndorsements(null, level, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.EndorsementOperation>());

                var proposals = ActiveOps.HasFlag(Operations.Proposals)
                    ? Repo.GetProposals(null, level, null, null, null, null, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.ProposalOperation>());

                var ballots = ActiveOps.HasFlag(Operations.Ballots)
                    ? Repo.GetBallots(null, level, null, null, null, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.BallotOperation>());

                var activations = ActiveOps.HasFlag(Operations.Activations)
                    ? Repo.GetActivations(null, level, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.ActivationOperation>());

                var doubleBaking = ActiveOps.HasFlag(Operations.DoubleBakings)
                    ? Repo.GetDoubleBakings(null, null, null, level, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.DoubleBakingOperation>());

                var doubleEndorsing = ActiveOps.HasFlag(Operations.DoubleEndorsings)
                    ? Repo.GetDoubleEndorsings(null, null, null, level, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.DoubleEndorsingOperation>());

                var revelations = ActiveOps.HasFlag(Operations.Revelations)
                    ? Repo.GetNonceRevelations(null, null, null, level, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.NonceRevelationOperation>());

                var delegations = ActiveOps.HasFlag(Operations.Delegations)
                    ? Repo.GetDelegations(null, null, null, null, null, level, null, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.DelegationOperation>());

                var originations = ActiveOps.HasFlag(Operations.Originations)
                    ? Repo.GetOriginations(null, null, null, null, null, null, null, null, level, null, null, null, null, limit, MichelineFormat.Json, symbols, true, true)
                    : Task.FromResult(Enumerable.Empty<Models.OriginationOperation>());

                var transactions = ActiveOps.HasFlag(Operations.Transactions)
                    ? Repo.GetTransactions(null, null, null, null, null, level, null, null, null, null, null, null, null, null, limit, MichelineFormat.Json, symbols, true, true)
                    : Task.FromResult(Enumerable.Empty<Models.TransactionOperation>());

                var reveals = ActiveOps.HasFlag(Operations.Reveals)
                    ? Repo.GetReveals(null, level, null, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.RevealOperation>());

                var migrations = ActiveOps.HasFlag(Operations.Migrations)
                    ? Repo.GetMigrations(null, null, null, level, null, null, null, limit, MichelineFormat.Json, symbols, true, true)
                    : Task.FromResult(Enumerable.Empty<Models.MigrationOperation>());

                var penalties = ActiveOps.HasFlag(Operations.RevelationPenalty)
                    ? Repo.GetRevelationPenalties(null, level, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.RevelationPenaltyOperation>());

                var baking = ActiveOps.HasFlag(Operations.Baking)
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
                foreach (var (connectionId, types) in Subs)
                {
                    var ops = new List<Operation>();

                    if (types.HasFlag(Operations.Endorsements))
                        ops.AddRange(endorsements.Result);

                    if (types.HasFlag(Operations.Ballots))
                        ops.AddRange(ballots.Result);

                    if (types.HasFlag(Operations.Proposals))
                        ops.AddRange(proposals.Result);

                    if (types.HasFlag(Operations.Activations))
                        ops.AddRange(activations.Result);

                    if (types.HasFlag(Operations.DoubleBakings))
                        ops.AddRange(doubleBaking.Result);

                    if (types.HasFlag(Operations.DoubleEndorsings))
                        ops.AddRange(doubleEndorsing.Result);

                    if (types.HasFlag(Operations.Revelations))
                        ops.AddRange(revelations.Result);

                    if (types.HasFlag(Operations.Delegations))
                        ops.AddRange(delegations.Result);

                    if (types.HasFlag(Operations.Originations))
                        ops.AddRange(originations.Result);

                    if (types.HasFlag(Operations.Transactions))
                        ops.AddRange(transactions.Result);

                    if (types.HasFlag(Operations.Reveals))
                        ops.AddRange(reveals.Result);

                    if (types.HasFlag(Operations.Migrations))
                        ops.AddRange(migrations.Result);

                    if (types.HasFlag(Operations.RevelationPenalty))
                        ops.AddRange(penalties.Result);

                    if (types.HasFlag(Operations.Baking))
                        ops.AddRange(baking.Result);

                    toSend[connectionId] = ops;
                }
                foreach (var (connectionId, subs) in AddressSubs)
                {
                    if (!toSend.ContainsKey(connectionId))
                        toSend[connectionId] = new List<Operation>();
                    
                    var included = Subs.ContainsKey(connectionId)
                        ? Subs[connectionId]
                        : Operations.None;

                    foreach (var (address, types) in subs)
                    {
                        var ops = toSend[connectionId];
                        var rest = types ^ (types & included);

                        if (rest.HasFlag(Operations.Endorsements))
                            ops.AddRange(endorsements.Result.Where(x =>
                                x.Delegate.Address == address));

                        if (rest.HasFlag(Operations.Ballots))
                            ops.AddRange(ballots.Result.Where(x =>
                                x.Delegate.Address == address));

                        if (rest.HasFlag(Operations.Proposals))
                            ops.AddRange(proposals.Result.Where(x =>
                                x.Delegate.Address == address));

                        if (rest.HasFlag(Operations.Activations))
                            ops.AddRange(activations.Result.Where(x => 
                                x.Account.Address == address));

                        if (rest.HasFlag(Operations.DoubleBakings))
                            ops.AddRange(doubleBaking.Result.Where(x =>
                                x.Accuser.Address == address ||
                                x.Offender.Address == address));

                        if (rest.HasFlag(Operations.DoubleEndorsings))
                            ops.AddRange(doubleEndorsing.Result.Where(x =>
                                x.Accuser.Address == address ||
                                x.Offender.Address == address));

                        if (rest.HasFlag(Operations.Revelations))
                            ops.AddRange(revelations.Result.Where(x =>
                                x.Sender.Address == address ||
                                x.Baker.Address == address));

                        if (rest.HasFlag(Operations.Delegations))
                            ops.AddRange(delegations.Result.Where(x =>
                                x.Initiator?.Address == address ||
                                x.Sender.Address == address ||
                                x.NewDelegate?.Address == address ||
                                x.PrevDelegate?.Address == address));

                        if (rest.HasFlag(Operations.Originations))
                            ops.AddRange(originations.Result.Where(x =>
                                x.Initiator?.Address == address ||
                                x.Sender.Address == address ||
                                x.ContractManager?.Address == address ||
                                x.ContractDelegate?.Address == address));

                        if (rest.HasFlag(Operations.Transactions))
                            ops.AddRange(transactions.Result.Where(x =>
                                x.Initiator?.Address == address ||
                                x.Sender.Address == address ||
                                x.Target?.Address == address));

                        if (rest.HasFlag(Operations.Reveals))
                            ops.AddRange(reveals.Result.Where(x =>
                                x.Sender.Address == address));
                    }
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

        public async Task Subscribe(IClientProxy client, string connectionId, string address, string types)
        {
            // TODO: validate base58 checksum
            if (address != null && !Regex.IsMatch(address, "^(tz1|tz2|tz3|KT1)[0-9A-z]{33}$"))
                throw new HubException("Invalid address");

            var ops = ParseOpTypes(types);
            Task sending = Task.CompletedTask;
            try
            {
                await Sema.WaitAsync();
                Logger.LogDebug("New subscription...");

                #region add to subs
                if (address == null)
                {
                    Subs[connectionId] = ops;
                }
                else
                {
                    if (!AddressSubs.TryGetValue(connectionId, out var subs))
                    {
                        subs = new Dictionary<string, Operations>();
                        AddressSubs.Add(connectionId, subs);
                    }

                    if (!subs.ContainsKey(address) && subs.Count >= Config.MaxAccountSubscriptions)
                        throw new HubException($"Subscriptions limit exceeded");

                    subs[address] = ops;
                }
                ActiveOps |= ops;
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

        public async Task Unsubscribe(string connectionId)
        {
            try
            {
                await Sema.WaitAsync();
                if (!Subs.ContainsKey(connectionId) && !AddressSubs.ContainsKey(connectionId)) return;
                Logger.LogDebug("Remove subscription...");
                
                Subs.Remove(connectionId);
                AddressSubs.Remove(connectionId);

                ActiveOps = Operations.None;
                foreach (var ops in Subs.Values)
                    ActiveOps |= ops;
                foreach (var sub in AddressSubs.Values)
                    foreach (var ops in sub.Values)
                        ActiveOps |= ops;

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
        
        private static Operations ParseOpTypes(string query)
        {
            if (string.IsNullOrEmpty(query))
                return Operations.Transactions;
            
            var res = Operations.None;
            foreach (var type in query.Split(','))
            {
                res |= type switch
                {
                    OpTypes.Endorsement => Operations.Endorsements,
                    
                    OpTypes.Ballot => Operations.Ballots,
                    OpTypes.Proposal => Operations.Proposals,
                    
                    OpTypes.Activation => Operations.Activations,
                    OpTypes.DoubleBaking => Operations.DoubleBakings,
                    OpTypes.DoubleEndorsing => Operations.DoubleEndorsings,
                    OpTypes.NonceRevelation => Operations.Revelations,

                    OpTypes.Delegation => Operations.Delegations,
                    OpTypes.Origination => Operations.Originations,
                    OpTypes.Transaction => Operations.Transactions,
                    OpTypes.Reveal => Operations.Reveals,

                    OpTypes.Migration => Operations.Migrations,
                    OpTypes.RevelationPenalty => Operations.RevelationPenalty,
                    OpTypes.Baking => Operations.Baking,

                    _ => throw new HubException($"Operation type `{type}` is not allowed")
                };
            }
            return res;
        }

        private static IEnumerable<Operation> Distinct(List<Operation> ops)
        {
            var hashset = new HashSet<int>(ops.Count);
            foreach (var op in ops)
            {
                if (!hashset.Contains(op.Id))
                {
                    hashset.Add(op.Id);
                    yield return op;
                }
            }
        }
    }
}