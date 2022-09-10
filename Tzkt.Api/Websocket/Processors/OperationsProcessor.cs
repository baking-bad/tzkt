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
        const string Group = "operations";
        const string Channel = "operations";
        static readonly SemaphoreSlim Sema = new(1, 1);

        static readonly Dictionary<Operations, TypeSub> TypeSubs = new();
        static readonly Dictionary<string, int> Limits = new();

        class TypeSub
        {
            public HashSet<string> Subs { get; set; }
            public Dictionary<int, HashSet<string>> CodeHashSubs { get; set; }
            public Dictionary<string, AddressSub> AddressSubs { get; set; }

            public bool Empty => Subs == null && CodeHashSubs == null && AddressSubs == null;
        }

        class AddressSub
        {
            public HashSet<string> Subs { get; set; }
            public Dictionary<int, HashSet<string>> CodeHashSubs { get; set; }

            public bool Empty => Subs == null && CodeHashSubs == null;
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
                        .Group(Group)
                        .SendReorg(Channel, State.ValidLevel));
                }
                #endregion

                if (State.ValidLevel == State.Current.Level)
                {
                    Logger.LogDebug("No operations to send");
                    return;
                }

                #region load operations
                Logger.LogDebug("Fetching operations from block {0} to block {1}", State.ValidLevel, State.Current.Level);

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
                var limit = 1_000_000; // crutch
                var symbols = Symbols.None;

                var endorsements = TypeSubs.TryGetValue(Operations.Endorsements, out var endorsementsSub)
                    ? Repo.GetEndorsements(null, level, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.EndorsementOperation>());

                var preendorsements = TypeSubs.TryGetValue(Operations.Preendorsements, out var preendorsementsSub)
                    ? Repo.GetPreendorsements(null, level, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.PreendorsementOperation>());

                var proposals = TypeSubs.TryGetValue(Operations.Proposals, out var proposalsSub)
                    ? Repo.GetProposals(null, level, null, null, null, null, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.ProposalOperation>());

                var ballots = TypeSubs.TryGetValue(Operations.Ballots, out var ballotsSub)
                    ? Repo.GetBallots(null, level, null, null, null, null, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.BallotOperation>());

                var activations = TypeSubs.TryGetValue(Operations.Activations, out var activationsSub)
                    ? Repo.GetActivations(null, level, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.ActivationOperation>());

                var doubleBaking = TypeSubs.TryGetValue(Operations.DoubleBakings, out var doubleBakingSub)
                    ? Repo.GetDoubleBakings(null, null, null, level, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.DoubleBakingOperation>());

                var doubleEndorsing = TypeSubs.TryGetValue(Operations.DoubleEndorsings, out var doubleEndorsingSub)
                    ? Repo.GetDoubleEndorsings(null, null, null, level, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.DoubleEndorsingOperation>());

                var doublePreendorsing = TypeSubs.TryGetValue(Operations.DoublePreendorsings, out var doublePreendorsingSub)
                    ? Repo.GetDoublePreendorsings(null, null, null, level, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.DoublePreendorsingOperation>());

                var revelations = TypeSubs.TryGetValue(Operations.Revelations, out var revelationsSub)
                    ? Repo.GetNonceRevelations(null, null, null, level, null, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.NonceRevelationOperation>());

                var vdfRevelations = TypeSubs.TryGetValue(Operations.VdfRevelation, out var vdfRevelationsSub)
                    ? Repo.GetVdfRevelations(null, level, null, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.VdfRevelationOperation>());

                var delegations = TypeSubs.TryGetValue(Operations.Delegations, out var delegationsSub)
                    ? Repo.GetDelegations(null, null, null, null, null, level, null, null, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.DelegationOperation>());

                var originations = TypeSubs.TryGetValue(Operations.Originations, out var originationsSub)
                    ? Repo.GetOriginations(null, null, null, null, null, null, null, null, null, level, null, null, null, null, null, null, limit, MichelineFormat.Json, symbols, true, true)
                    : Task.FromResult(Enumerable.Empty<Models.OriginationOperation>());

                var transactions = TypeSubs.TryGetValue(Operations.Transactions, out var transactionsSub)
                    ? Repo.GetTransactions(null, null, null, null, null, null, level, null, null, null, null, null, null, null, null, null, null, limit, MichelineFormat.Json, symbols, true, true)
                    : Task.FromResult(Enumerable.Empty<Models.TransactionOperation>());

                var reveals = TypeSubs.TryGetValue(Operations.Reveals, out var revealsSub)
                    ? Repo.GetReveals(null, level, null, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.RevealOperation>());

                var registerConstants = TypeSubs.TryGetValue(Operations.RegisterConstant, out var registerConstantsSub)
                    ? Repo.GetRegisterConstants(null, null, level, null, null, null, null, limit, MichelineFormat.Json, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.RegisterConstantOperation>());

                var setDepositsLimits = TypeSubs.TryGetValue(Operations.SetDepositsLimits, out var setDepositsLimitsSub)
                    ? Repo.GetSetDepositsLimits(null, level, null, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.SetDepositsLimitOperation>());

                var transferTicketOps = TypeSubs.TryGetValue(Operations.TransferTicket, out var transferTicketSub)
                    ? Repo.GetTransferTicketOps(null, null, null, null, level, null, null, null, null, limit, MichelineFormat.Json, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.TransferTicketOperation>());

                var txRollupCommitOps = TypeSubs.TryGetValue(Operations.TxRollupCommit, out var txRollupCommitSub)
                    ? Repo.GetTxRollupCommitOps(null, null, level, null, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.TxRollupCommitOperation>());

                var txRollupDispatchTicketsOps = TypeSubs.TryGetValue(Operations.TxRollupDispatchTickets, out var txRollupDispatchTicketsSub)
                    ? Repo.GetTxRollupDispatchTicketsOps(null, null, level, null, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.TxRollupDispatchTicketsOperation>());

                var txRollupFinalizeCommitmentOps = TypeSubs.TryGetValue(Operations.TxRollupFinalizeCommitment, out var txRollupFinalizeCommitmentSub)
                    ? Repo.GetTxRollupFinalizeCommitmentOps(null, null, level, null, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.TxRollupFinalizeCommitmentOperation>());

                var txRollupOriginationOps = TypeSubs.TryGetValue(Operations.TxRollupOrigination, out var txRollupOriginationSub)
                    ? Repo.GetTxRollupOriginationOps(null, null, level, null, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.TxRollupOriginationOperation>());

                var txRollupRejectionOps = TypeSubs.TryGetValue(Operations.TxRollupRejection, out var txRollupRejectionSub)
                    ? Repo.GetTxRollupRejectionOps(null, null, null, null, level, null, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.TxRollupRejectionOperation>());

                var txRollupRemoveCommitmentOps = TypeSubs.TryGetValue(Operations.TxRollupRemoveCommitment, out var txRollupRemoveCommitmentSub)
                    ? Repo.GetTxRollupRemoveCommitmentOps(null, null, level, null, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.TxRollupRemoveCommitmentOperation>());

                var txRollupReturnBondOps = TypeSubs.TryGetValue(Operations.TxRollupReturnBond, out var txRollupReturnBondSub)
                    ? Repo.GetTxRollupReturnBondOps(null, null, level, null, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.TxRollupReturnBondOperation>());

                var txRollupSubmitBatchOps = TypeSubs.TryGetValue(Operations.TxRollupSubmitBatch, out var txRollupSubmitBatchSub)
                    ? Repo.GetTxRollupSubmitBatchOps(null, null, level, null, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.TxRollupSubmitBatchOperation>());

                var increasePaidStorageOps = TypeSubs.TryGetValue(Operations.IncreasePaidStorage, out var increasePaidStorageSubs)
                    ? Repo.GetIncreasePaidStorageOps(null, null, level, null, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.IncreasePaidStorageOperation>());

                var migrations = TypeSubs.TryGetValue(Operations.Migrations, out var migrationsSub)
                    ? Repo.GetMigrations(null, null, null, null, level, null, null, null, limit, MichelineFormat.Json, symbols, true, true)
                    : Task.FromResult(Enumerable.Empty<Models.MigrationOperation>());

                var penalties = TypeSubs.TryGetValue(Operations.RevelationPenalty, out var penaltiesSub)
                    ? Repo.GetRevelationPenalties(null, level, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.RevelationPenaltyOperation>());

                var baking = TypeSubs.TryGetValue(Operations.Baking, out var bakingSub)
                    ? Repo.GetBakings(null, null, null, level, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.BakingOperation>());

                var endorsingRewards = TypeSubs.TryGetValue(Operations.EndorsingRewards, out var endorsingRewardsSub)
                    ? Repo.GetEndorsingRewards(null, level, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.EndorsingRewardOperation>());

                await Task.WhenAll(
                    endorsements,
                    preendorsements,
                    proposals,
                    ballots,
                    activations,
                    doubleBaking,
                    doubleEndorsing,
                    doublePreendorsing,
                    revelations,
                    vdfRevelations,
                    delegations,
                    originations,
                    transactions,
                    reveals,
                    registerConstants,
                    setDepositsLimits,
                    transferTicketOps,
                    txRollupCommitOps,
                    txRollupDispatchTicketsOps,
                    txRollupFinalizeCommitmentOps,
                    txRollupOriginationOps,
                    txRollupRejectionOps,
                    txRollupRemoveCommitmentOps,
                    txRollupReturnBondOps,
                    txRollupSubmitBatchOps,
                    increasePaidStorageOps,
                    migrations,
                    penalties,
                    baking,
                    endorsingRewards);
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
                    if (endorsementsSub.Subs != null)
                        AddRange(endorsementsSub.Subs, endorsements.Result);

                    if (endorsementsSub.AddressSubs != null)
                        foreach (var op in endorsements.Result)
                            if (endorsementsSub.AddressSubs.TryGetValue(op.Delegate.Address, out var delegateSubs))
                                Add(delegateSubs.Subs, op);
                }

                if (preendorsements.Result.Any())
                {
                    if (preendorsementsSub.Subs != null)
                        AddRange(preendorsementsSub.Subs, preendorsements.Result);

                    if (preendorsementsSub.AddressSubs != null)
                        foreach (var op in preendorsements.Result)
                            if (preendorsementsSub.AddressSubs.TryGetValue(op.Delegate.Address, out var delegateSubs))
                                Add(delegateSubs.Subs, op);
                }

                if (ballots.Result.Any())
                {
                    if (ballotsSub.Subs != null)
                        AddRange(ballotsSub.Subs, ballots.Result);

                    if (ballotsSub.AddressSubs != null)
                        foreach (var op in ballots.Result)
                            if (ballotsSub.AddressSubs.TryGetValue(op.Delegate.Address, out var delegateSubs))
                                Add(delegateSubs.Subs, op);
                }

                if (proposals.Result.Any())
                {
                    if (proposalsSub.Subs != null)
                        AddRange(proposalsSub.Subs, proposals.Result);

                    if (proposalsSub.AddressSubs != null)
                        foreach (var op in proposals.Result)
                            if (proposalsSub.AddressSubs.TryGetValue(op.Delegate.Address, out var delegateSubs))
                                Add(delegateSubs.Subs, op);
                }

                if (activations.Result.Any())
                {
                    if (activationsSub.Subs != null)
                        AddRange(activationsSub.Subs, activations.Result);

                    if (activationsSub.AddressSubs != null)
                        foreach (var op in activations.Result)
                            if (activationsSub.AddressSubs.TryGetValue(op.Account.Address, out var accountSubs))
                                Add(accountSubs.Subs, op);
                }

                if (doubleBaking.Result.Any())
                {
                    if (doubleBakingSub.Subs != null)
                        AddRange(doubleBakingSub.Subs, doubleBaking.Result);

                    if (doubleBakingSub.AddressSubs != null)
                        foreach (var op in doubleBaking.Result)
                        {
                            if (doubleBakingSub.AddressSubs.TryGetValue(op.Accuser.Address, out var accuserSubs))
                                Add(accuserSubs.Subs, op);

                            if (doubleBakingSub.AddressSubs.TryGetValue(op.Offender.Address, out var offenderSubs))
                                Add(offenderSubs.Subs, op);
                        }
                }

                if (doubleEndorsing.Result.Any())
                {
                    if (doubleEndorsingSub.Subs != null)
                        AddRange(doubleEndorsingSub.Subs, doubleEndorsing.Result);

                    if (doubleEndorsingSub.AddressSubs != null)
                        foreach (var op in doubleEndorsing.Result)
                        {
                            if (doubleEndorsingSub.AddressSubs.TryGetValue(op.Accuser.Address, out var accuserSubs))
                                Add(accuserSubs.Subs, op);

                            if (doubleEndorsingSub.AddressSubs.TryGetValue(op.Offender.Address, out var offenderSubs))
                                Add(offenderSubs.Subs, op);
                        }
                }

                if (doublePreendorsing.Result.Any())
                {
                    if (doublePreendorsingSub.Subs != null)
                        AddRange(doublePreendorsingSub.Subs, doublePreendorsing.Result);

                    if (doublePreendorsingSub.AddressSubs != null)
                        foreach (var op in doublePreendorsing.Result)
                        {
                            if (doublePreendorsingSub.AddressSubs.TryGetValue(op.Accuser.Address, out var accuserSubs))
                                Add(accuserSubs.Subs, op);

                            if (doublePreendorsingSub.AddressSubs.TryGetValue(op.Offender.Address, out var offenderSubs))
                                Add(offenderSubs.Subs, op);
                        }
                }

                if (revelations.Result.Any())
                {
                    if (revelationsSub.Subs != null)
                        AddRange(revelationsSub.Subs, revelations.Result);

                    if (revelationsSub.AddressSubs != null)
                        foreach (var op in revelations.Result)
                        {
                            if (revelationsSub.AddressSubs.TryGetValue(op.Baker.Address, out var bakerSubs))
                                Add(bakerSubs.Subs, op);

                            if (revelationsSub.AddressSubs.TryGetValue(op.Sender.Address, out var senderSubs))
                                Add(senderSubs.Subs, op);
                        }
                }

                if (vdfRevelations.Result.Any())
                {
                    if (vdfRevelationsSub.Subs != null)
                        AddRange(vdfRevelationsSub.Subs, vdfRevelations.Result);

                    if (vdfRevelationsSub.AddressSubs != null)
                        foreach (var op in vdfRevelations.Result)
                            if (vdfRevelationsSub.AddressSubs.TryGetValue(op.Baker.Address, out var bakerSubs))
                                Add(bakerSubs.Subs, op);
                }

                if (delegations.Result.Any())
                {
                    if (delegationsSub.Subs != null)
                        AddRange(delegationsSub.Subs, delegations.Result);

                    void AddByCodeHash(Dictionary<int, HashSet<string>> subs, Models.DelegationOperation op)
                    {
                        if (op.SenderCodeHash != null && subs.TryGetValue((int)op.SenderCodeHash, out var senderCodeHashSubs))
                            Add(senderCodeHashSubs, op);
                    }

                    if (delegationsSub.CodeHashSubs != null)
                        foreach (var op in delegations.Result)
                            AddByCodeHash(delegationsSub.CodeHashSubs, op);

                    if (delegationsSub.AddressSubs != null)
                        foreach (var op in delegations.Result)
                        {
                            if (op.Initiator != null && delegationsSub.AddressSubs.TryGetValue(op.Initiator.Address, out var initiatorSubs))
                            {
                                if (initiatorSubs.Subs != null)
                                    Add(initiatorSubs.Subs, op);

                                if (initiatorSubs.CodeHashSubs != null)
                                    AddByCodeHash(initiatorSubs.CodeHashSubs, op);
                            }

                            if (delegationsSub.AddressSubs.TryGetValue(op.Sender.Address, out var senderSubs))
                            {
                                if (senderSubs.Subs != null)
                                    Add(senderSubs.Subs, op);

                                if (senderSubs.CodeHashSubs != null)
                                    AddByCodeHash(senderSubs.CodeHashSubs, op);
                            }

                            if (op.PrevDelegate != null && delegationsSub.AddressSubs.TryGetValue(op.PrevDelegate.Address, out var prevSubs))
                            {
                                if (prevSubs.Subs != null)
                                    Add(prevSubs.Subs, op);

                                if (prevSubs.CodeHashSubs != null)
                                    AddByCodeHash(prevSubs.CodeHashSubs, op);
                            }

                            if (op.NewDelegate != null && delegationsSub.AddressSubs.TryGetValue(op.NewDelegate.Address, out var newSubs))
                            {
                                if (newSubs.Subs != null)
                                    Add(newSubs.Subs, op);

                                if (newSubs.CodeHashSubs != null)
                                    AddByCodeHash(newSubs.CodeHashSubs, op);
                            }
                        }
                }

                if (originations.Result.Any())
                {
                    if (originationsSub.Subs != null)
                        AddRange(originationsSub.Subs, originations.Result);

                    void AddByCodeHash(Dictionary<int, HashSet<string>> subs, Models.OriginationOperation op)
                    {
                        if (op.SenderCodeHash != null && subs.TryGetValue((int)op.SenderCodeHash, out var senderCodeHashSubs))
                            Add(senderCodeHashSubs, op);

                        if (op.OriginatedContract?.CodeHash != null && subs.TryGetValue(op.OriginatedContract.CodeHash, out var targetCodeHash))
                            Add(targetCodeHash, op);
                    }

                    if (originationsSub.CodeHashSubs != null)
                        foreach (var op in originations.Result)
                            AddByCodeHash(originationsSub.CodeHashSubs, op);

                    if (originationsSub.AddressSubs != null)
                        foreach (var op in originations.Result)
                        {
                            if (op.Initiator != null && originationsSub.AddressSubs.TryGetValue(op.Initiator.Address, out var initiatorSubs))
                            {
                                if (initiatorSubs.Subs != null)
                                    Add(initiatorSubs.Subs, op);

                                if (initiatorSubs.CodeHashSubs != null)
                                    AddByCodeHash(initiatorSubs.CodeHashSubs, op);
                            }

                            if (originationsSub.AddressSubs.TryGetValue(op.Sender.Address, out var senderSubs))
                            {
                                if (senderSubs.Subs != null)
                                    Add(senderSubs.Subs, op);

                                if (senderSubs.CodeHashSubs != null)
                                    AddByCodeHash(senderSubs.CodeHashSubs, op);
                            }

                            if (op.ContractManager != null && originationsSub.AddressSubs.TryGetValue(op.ContractManager.Address, out var managerSubs))
                            {
                                if (managerSubs.Subs != null)
                                    Add(managerSubs.Subs, op);

                                if (managerSubs.CodeHashSubs != null)
                                    AddByCodeHash(managerSubs.CodeHashSubs, op);
                            }

                            if (op.ContractDelegate != null && originationsSub.AddressSubs.TryGetValue(op.ContractDelegate.Address, out var delegateSubs))
                            {
                                if (delegateSubs.Subs != null)
                                    Add(delegateSubs.Subs, op);

                                if (delegateSubs.CodeHashSubs != null)
                                    AddByCodeHash(delegateSubs.CodeHashSubs, op);
                            }
                        }
                }

                if (transactions.Result.Any())
                {
                    if (transactionsSub.Subs != null)
                        AddRange(transactionsSub.Subs, transactions.Result);

                    void AddByCodeHash(Dictionary<int, HashSet<string>> subs, Models.TransactionOperation op)
                    {
                        if (op.SenderCodeHash != null && subs.TryGetValue((int)op.SenderCodeHash, out var senderCodeHashSubs))
                            Add(senderCodeHashSubs, op);

                        if (op.TargetCodeHash != null && subs.TryGetValue((int)op.TargetCodeHash, out var targetCodeHash))
                            Add(targetCodeHash, op);
                    }

                    if (transactionsSub.CodeHashSubs != null)
                        foreach (var op in transactions.Result)
                            AddByCodeHash(transactionsSub.CodeHashSubs, op);

                    if (transactionsSub.AddressSubs != null)
                        foreach (var op in transactions.Result)
                        {
                            if (op.Initiator != null && transactionsSub.AddressSubs.TryGetValue(op.Initiator.Address, out var initiatorSubs))
                            {
                                if (initiatorSubs.Subs != null)
                                    Add(initiatorSubs.Subs, op);

                                if (initiatorSubs.CodeHashSubs != null)
                                    AddByCodeHash(initiatorSubs.CodeHashSubs, op);
                            }

                            if (transactionsSub.AddressSubs.TryGetValue(op.Sender.Address, out var senderSubs))
                            {
                                if (senderSubs.Subs != null)
                                    Add(senderSubs.Subs, op);

                                if (senderSubs.CodeHashSubs != null)
                                    AddByCodeHash(senderSubs.CodeHashSubs, op);
                            }

                            if (op.Target != null && transactionsSub.AddressSubs.TryGetValue(op.Target.Address, out var targetSubs))
                            {
                                if (targetSubs.Subs != null)
                                    Add(targetSubs.Subs, op);

                                if (targetSubs.CodeHashSubs != null)
                                    AddByCodeHash(targetSubs.CodeHashSubs, op);
                            }
                        }
                }

                if (reveals.Result.Any())
                {
                    if (revealsSub.Subs != null)
                        AddRange(revealsSub.Subs, reveals.Result);

                    if (revealsSub.AddressSubs != null)
                        foreach (var op in reveals.Result)
                            if (revealsSub.AddressSubs.TryGetValue(op.Sender.Address, out var senderSubs))
                                Add(senderSubs.Subs, op);
                }

                if (registerConstants.Result.Any())
                {
                    if (registerConstantsSub.Subs != null)
                        AddRange(registerConstantsSub.Subs, registerConstants.Result);

                    if (registerConstantsSub.AddressSubs != null)
                        foreach (var op in registerConstants.Result)
                            if (registerConstantsSub.AddressSubs.TryGetValue(op.Sender.Address, out var senderSubs))
                                Add(senderSubs.Subs, op);
                }

                if (setDepositsLimits.Result.Any())
                {
                    if (setDepositsLimitsSub.Subs != null)
                        AddRange(setDepositsLimitsSub.Subs, setDepositsLimits.Result);

                    if (setDepositsLimitsSub.AddressSubs != null)
                        foreach (var op in setDepositsLimits.Result)
                            if (setDepositsLimitsSub.AddressSubs.TryGetValue(op.Sender.Address, out var senderSubs))
                                Add(senderSubs.Subs, op);
                }

                if (transferTicketOps.Result.Any())
                {
                    if (transferTicketSub.Subs != null)
                        AddRange(transferTicketSub.Subs, transferTicketOps.Result);

                    if (transferTicketSub.AddressSubs != null)
                        foreach (var op in transferTicketOps.Result)
                        {
                            if (transferTicketSub.AddressSubs.TryGetValue(op.Sender.Address, out var senderSubs))
                                Add(senderSubs.Subs, op);

                            if (transferTicketSub.AddressSubs.TryGetValue(op.Target.Address, out var targetSubs))
                                Add(targetSubs.Subs, op);

                            if (transferTicketSub.AddressSubs.TryGetValue(op.Ticketer.Address, out var ticketerSubs))
                                Add(ticketerSubs.Subs, op);
                        }
                }

                if (txRollupCommitOps.Result.Any())
                {
                    if (txRollupCommitSub.Subs != null)
                        AddRange(txRollupCommitSub.Subs, txRollupCommitOps.Result);

                    if (txRollupCommitSub.AddressSubs != null)
                        foreach (var op in txRollupCommitOps.Result)
                        {
                            if (txRollupCommitSub.AddressSubs.TryGetValue(op.Sender.Address, out var senderSubs))
                                Add(senderSubs.Subs, op);

                            if (txRollupCommitSub.AddressSubs.TryGetValue(op.Rollup.Address, out var rollupSubs))
                                Add(rollupSubs.Subs, op);
                        }
                }

                if (txRollupDispatchTicketsOps.Result.Any())
                {
                    if (txRollupDispatchTicketsSub.Subs != null)
                        AddRange(txRollupDispatchTicketsSub.Subs, txRollupDispatchTicketsOps.Result);

                    if (txRollupDispatchTicketsSub.AddressSubs != null)
                        foreach (var op in txRollupDispatchTicketsOps.Result)
                        {
                            if (txRollupDispatchTicketsSub.AddressSubs.TryGetValue(op.Sender.Address, out var senderSubs))
                                Add(senderSubs.Subs, op);

                            if (txRollupDispatchTicketsSub.AddressSubs.TryGetValue(op.Rollup.Address, out var rollupSubs))
                                Add(rollupSubs.Subs, op);
                        }
                }

                if (txRollupFinalizeCommitmentOps.Result.Any())
                {
                    if (txRollupFinalizeCommitmentSub.Subs != null)
                        AddRange(txRollupFinalizeCommitmentSub.Subs, txRollupFinalizeCommitmentOps.Result);

                    if (txRollupFinalizeCommitmentSub.AddressSubs != null)
                        foreach (var op in txRollupFinalizeCommitmentOps.Result)
                        {
                            if (txRollupFinalizeCommitmentSub.AddressSubs.TryGetValue(op.Sender.Address, out var senderSubs))
                                Add(senderSubs.Subs, op);

                            if (txRollupFinalizeCommitmentSub.AddressSubs.TryGetValue(op.Rollup.Address, out var rollupSubs))
                                Add(rollupSubs.Subs, op);
                        }
                }

                if (txRollupOriginationOps.Result.Any())
                {
                    if (txRollupOriginationSub.Subs != null)
                        AddRange(txRollupOriginationSub.Subs, txRollupOriginationOps.Result);

                    if (txRollupOriginationSub.AddressSubs != null)
                        foreach (var op in txRollupOriginationOps.Result)
                        {
                            if (txRollupOriginationSub.AddressSubs.TryGetValue(op.Sender.Address, out var senderSubs))
                                Add(senderSubs.Subs, op);

                            if (txRollupOriginationSub.AddressSubs.TryGetValue(op.Rollup.Address, out var rollupSubs))
                                Add(rollupSubs.Subs, op);
                        }
                }

                if (txRollupRejectionOps.Result.Any())
                {
                    if (txRollupRejectionSub.Subs != null)
                        AddRange(txRollupRejectionSub.Subs, txRollupRejectionOps.Result);

                    if (txRollupRejectionSub.AddressSubs != null)
                        foreach (var op in txRollupRejectionOps.Result)
                        {
                            if (txRollupRejectionSub.AddressSubs.TryGetValue(op.Sender.Address, out var senderSubs))
                                Add(senderSubs.Subs, op);

                            if (txRollupRejectionSub.AddressSubs.TryGetValue(op.Committer.Address, out var committerSubs))
                                Add(committerSubs.Subs, op);

                            if (txRollupRejectionSub.AddressSubs.TryGetValue(op.Rollup.Address, out var rollupSubs))
                                Add(rollupSubs.Subs, op);
                        }
                }

                if (txRollupRemoveCommitmentOps.Result.Any())
                {
                    if (txRollupRemoveCommitmentSub.Subs != null)
                        AddRange(txRollupRemoveCommitmentSub.Subs, txRollupRemoveCommitmentOps.Result);

                    if (txRollupRemoveCommitmentSub.AddressSubs != null)
                        foreach (var op in txRollupRemoveCommitmentOps.Result)
                        {
                            if (txRollupRemoveCommitmentSub.AddressSubs.TryGetValue(op.Sender.Address, out var senderSubs))
                                Add(senderSubs.Subs, op);

                            if (txRollupRemoveCommitmentSub.AddressSubs.TryGetValue(op.Rollup.Address, out var rollupSubs))
                                Add(rollupSubs.Subs, op);
                        }
                }

                if (txRollupReturnBondOps.Result.Any())
                {
                    if (txRollupReturnBondSub.Subs != null)
                        AddRange(txRollupReturnBondSub.Subs, txRollupReturnBondOps.Result);

                    if (txRollupReturnBondSub.AddressSubs != null)
                        foreach (var op in txRollupReturnBondOps.Result)
                        {
                            if (txRollupReturnBondSub.AddressSubs.TryGetValue(op.Sender.Address, out var senderSubs))
                                Add(senderSubs.Subs, op);

                            if (txRollupReturnBondSub.AddressSubs.TryGetValue(op.Rollup.Address, out var rollupSubs))
                                Add(rollupSubs.Subs, op);
                        }
                }

                if (txRollupSubmitBatchOps.Result.Any())
                {
                    if (txRollupSubmitBatchSub.Subs != null)
                        AddRange(txRollupSubmitBatchSub.Subs, txRollupSubmitBatchOps.Result);

                    if (txRollupSubmitBatchSub.AddressSubs != null)
                        foreach (var op in txRollupSubmitBatchOps.Result)
                        {
                            if (txRollupSubmitBatchSub.AddressSubs.TryGetValue(op.Sender.Address, out var senderSubs))
                                Add(senderSubs.Subs, op);

                            if (txRollupSubmitBatchSub.AddressSubs.TryGetValue(op.Rollup.Address, out var rollupSubs))
                                Add(rollupSubs.Subs, op);
                        }
                }

                if (increasePaidStorageOps.Result.Any())
                {
                    if (increasePaidStorageSubs.Subs != null)
                        AddRange(increasePaidStorageSubs.Subs, increasePaidStorageOps.Result);

                    if (increasePaidStorageSubs.AddressSubs != null)
                        foreach (var op in increasePaidStorageOps.Result)
                        {
                            if (increasePaidStorageSubs.AddressSubs.TryGetValue(op.Sender.Address, out var senderSubs))
                                Add(senderSubs.Subs, op);

                            if (increasePaidStorageSubs.AddressSubs.TryGetValue(op.Contract.Address, out var contractSubs))
                                Add(contractSubs.Subs, op);
                        }
                }

                if (migrations.Result.Any())
                {
                    if (migrationsSub.Subs != null)
                        AddRange(migrationsSub.Subs, migrations.Result);

                    if (migrationsSub.AddressSubs != null)
                        foreach (var op in migrations.Result)
                            if (migrationsSub.AddressSubs.TryGetValue(op.Account.Address, out var accountSubs))
                                Add(accountSubs.Subs, op);
                }

                if (penalties.Result.Any())
                {
                    if (penaltiesSub.Subs != null)
                        AddRange(penaltiesSub.Subs, penalties.Result);

                    if (penaltiesSub.AddressSubs != null)
                        foreach (var op in penalties.Result)
                            if (penaltiesSub.AddressSubs.TryGetValue(op.Baker.Address, out var bakerSubs))
                                Add(bakerSubs.Subs, op);
                }

                if (baking.Result.Any())
                {
                    if (bakingSub.Subs != null)
                        AddRange(bakingSub.Subs, baking.Result);

                    if (bakingSub.AddressSubs != null)
                        foreach (var op in baking.Result)
                        {
                            if (bakingSub.AddressSubs.TryGetValue(op.Proposer.Address, out var proposerSubs))
                                Add(proposerSubs.Subs, op);

                            if (bakingSub.AddressSubs.TryGetValue(op.Producer.Address, out var producerSubs))
                                Add(producerSubs.Subs, op);
                        }
                }

                if (endorsingRewards.Result.Any())
                {
                    if (endorsingRewardsSub.Subs != null)
                        AddRange(endorsingRewardsSub.Subs, endorsingRewards.Result);

                    if (endorsingRewardsSub.AddressSubs != null)
                        foreach (var op in endorsingRewards.Result)
                            if (endorsingRewardsSub.AddressSubs.TryGetValue(op.Baker.Address, out var bakerSubs))
                                Add(bakerSubs.Subs, op);
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
                        .SendData(Channel, data, State.Current.Level));

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

        public async Task<int> Subscribe(IClientProxy client, string connectionId, OperationsParameter parameter)
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
                    if (!TypeSubs.TryGetValue(type, out var typeSub))
                    {
                        typeSub = new();
                        TypeSubs.Add(type, typeSub);
                    }
                    if (parameter.Address != null)
                    {
                        typeSub.AddressSubs ??= new();
                        if (!typeSub.AddressSubs.TryGetValue(parameter.Address, out var addressSub))
                        {
                            addressSub = new();
                            typeSub.AddressSubs.Add(parameter.Address, addressSub);
                        }
                        if (parameter.CodeHash != null)
                        {
                            addressSub.CodeHashSubs ??= new();
                            TryAdd(addressSub.CodeHashSubs, (int)parameter.CodeHash, connectionId);
                        }
                        else
                        {
                            addressSub.Subs ??= new();
                            TryAdd(addressSub.Subs, connectionId);
                        }
                    }
                    else if (parameter.CodeHash != null)
                    {
                        typeSub.CodeHashSubs ??= new();
                        TryAdd(typeSub.CodeHashSubs, (int)parameter.CodeHash, connectionId);
                    }
                    else
                    {
                        typeSub.Subs ??= new();
                        TryAdd(typeSub.Subs, connectionId);
                    }
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

                foreach (var (type, typeSub) in TypeSubs)
                {
                    typeSub.Subs = TryRemove(typeSub.Subs, connectionId);
                    typeSub.CodeHashSubs = TryRemove(typeSub.CodeHashSubs, connectionId);
                    if (typeSub.AddressSubs != null)
                    {
                        foreach (var (address, addressSub) in typeSub.AddressSubs)
                        {
                            addressSub.Subs = TryRemove(addressSub.Subs, connectionId);
                            addressSub.CodeHashSubs = TryRemove(addressSub.CodeHashSubs, connectionId);
                            
                            if (addressSub.Empty)
                                typeSub.AddressSubs.Remove(address);
                        }
                        
                        if (typeSub.AddressSubs.Count == 0)
                            typeSub.AddressSubs = null;
                    }

                    if (typeSub.Empty)
                        TypeSubs.Remove(type);
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

        private static IEnumerable<Operation> Distinct(List<Operation> ops)
        {
            var set = new HashSet<long>(ops.Count);
            foreach (var op in ops)
                if (set.Add(op.Id))
                    yield return op;
        }
    }
}