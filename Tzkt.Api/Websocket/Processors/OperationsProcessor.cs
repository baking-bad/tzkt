﻿using Microsoft.AspNetCore.SignalR;
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

        static readonly Dictionary<Operations, TypeSub> TypeSubs = [];
        static readonly Dictionary<string, int> Limits = [];

        class TypeSub
        {
            public HashSet<string>? Subs { get; set; }
            public Dictionary<int, HashSet<string>>? CodeHashSubs { get; set; }
            public Dictionary<string, AddressSub>? AddressSubs { get; set; }

            public bool Empty => Subs == null && CodeHashSubs == null && AddressSubs == null;
        }

        class AddressSub
        {
            public HashSet<string>? Subs { get; set; }
            public Dictionary<int, HashSet<string>>? CodeHashSubs { get; set; }

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
                    Logger.LogDebug("Sending reorg message with state {state}", State.ValidLevel);
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
                Logger.LogDebug("Fetching operations from block {valid} to block {current}", State.ValidLevel, State.Current.Level);

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
                    ? Repo.GetEndorsements(null, null, level, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.EndorsementOperation>());

                var preendorsements = TypeSubs.TryGetValue(Operations.Preendorsements, out var preendorsementsSub)
                    ? Repo.GetPreendorsements(null, null, level, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.PreendorsementOperation>());

                var proposals = TypeSubs.TryGetValue(Operations.Proposals, out var proposalsSub)
                    ? Repo.GetProposals(null, null, level, null, null, null, null, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.ProposalOperation>());

                var ballots = TypeSubs.TryGetValue(Operations.Ballots, out var ballotsSub)
                    ? Repo.GetBallots(null, null, level, null, null, null, null, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.BallotOperation>());

                var activations = TypeSubs.TryGetValue(Operations.Activations, out var activationsSub)
                    ? Repo.GetActivations(null, null, level, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.ActivationOperation>());

                var dalEntrapmentEvidences = TypeSubs.TryGetValue(Operations.DalEntrapmentEvidence, out var dalEntrapmentEvidencesSub)
                    ? Repo.GetDalEntrapmentEvidences(null, null, null, null, null, level, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.DalEntrapmentEvidenceOperation>());

                var doubleBaking = TypeSubs.TryGetValue(Operations.DoubleBakings, out var doubleBakingSub)
                    ? Repo.GetDoubleBakings(null, null, null, null, null, level, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.DoubleBakingOperation>());

                var doubleEndorsing = TypeSubs.TryGetValue(Operations.DoubleEndorsings, out var doubleEndorsingSub)
                    ? Repo.GetDoubleEndorsings(null, null, null, null, null, level, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.DoubleEndorsingOperation>());

                var doublePreendorsing = TypeSubs.TryGetValue(Operations.DoublePreendorsings, out var doublePreendorsingSub)
                    ? Repo.GetDoublePreendorsings(null, null, null, null, null, level, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.DoublePreendorsingOperation>());

                var revelations = TypeSubs.TryGetValue(Operations.Revelations, out var revelationsSub)
                    ? Repo.GetNonceRevelations(null, null, null, null, level, null, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.NonceRevelationOperation>());

                var vdfRevelations = TypeSubs.TryGetValue(Operations.VdfRevelation, out var vdfRevelationsSub)
                    ? Repo.GetVdfRevelations(null, null, level, null, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.VdfRevelationOperation>());

                var delegations = TypeSubs.TryGetValue(Operations.Delegations, out var delegationsSub)
                    ? Repo.GetDelegations(null, null, null, null, null, null, null, level, null, null, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.DelegationOperation>());

                var originations = TypeSubs.TryGetValue(Operations.Originations, out var originationsSub)
                    ? Repo.GetOriginations(null, null, null, null, null, null, null, null, null, level, null, null, null, null, null, null, limit, MichelineFormat.Json, symbols, true, true)
                    : Task.FromResult(Enumerable.Empty<Models.OriginationOperation>());

                var transactions = TypeSubs.TryGetValue(Operations.Transactions, out var transactionsSub)
                    ? Repo.GetTransactions(null, null, null, null, null, null, null, level, null, null, null, null, null, null, null, null, null, null, limit, MichelineFormat.Json, symbols, true, true)
                    : Task.FromResult(Enumerable.Empty<Models.TransactionOperation>());

                var reveals = TypeSubs.TryGetValue(Operations.Reveals, out var revealsSub)
                    ? Repo.GetReveals(null, null, level, null, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.RevealOperation>());

                var registerConstants = TypeSubs.TryGetValue(Operations.RegisterConstant, out var registerConstantsSub)
                    ? Repo.GetRegisterConstants(null, null, null, level, null, null, null, null, limit, MichelineFormat.Json, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.RegisterConstantOperation>());

                var setDepositsLimits = TypeSubs.TryGetValue(Operations.SetDepositsLimits, out var setDepositsLimitsSub)
                    ? Repo.GetSetDepositsLimits(null, null, level, null, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.SetDepositsLimitOperation>());

                var transferTicketOps = TypeSubs.TryGetValue(Operations.TransferTicket, out var transferTicketSub)
                    ? Repo.GetTransferTicketOps(null, null, null, null, null, null, level, null, null, null, null, limit, MichelineFormat.Json, symbols)
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
                    ? Repo.GetIncreasePaidStorageOps(null, null, null, level, null, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.IncreasePaidStorageOperation>());

                var updateConsensusKeyOps = TypeSubs.TryGetValue(Operations.UpdateConsensusKey, out var updateConsensusKeySubs)
                    ? Repo.GetUpdateConsensusKeys(null, null, null, null, level, null, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.UpdateConsensusKeyOperation>());

                var drainDelegateOps = TypeSubs.TryGetValue(Operations.DrainDelegate, out var drainDelegateSubs)
                    ? Repo.GetDrainDelegates(null, null, null, null, level, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.DrainDelegateOperation>());

                var srAddMessagesOps = TypeSubs.TryGetValue(Operations.SmartRollupAddMessages, out var srAddMessagesSubs)
                    ? Repo.GetSmartRollupAddMessagesOps(new() { level = level }, new() { limit = -1 }, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.SmartRollupAddMessagesOperation>());

                var srCementOps = TypeSubs.TryGetValue(Operations.SmartRollupCement, out var srCementSubs)
                    ? Repo.GetSmartRollupCementOps(new() { level = level }, new() { limit = -1 }, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.SmartRollupCementOperation>());

                var srExecuteOps = TypeSubs.TryGetValue(Operations.SmartRollupExecute, out var srExecuteSubs)
                    ? Repo.GetSmartRollupExecuteOps(new() { level = level }, new() { limit = -1 }, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.SmartRollupExecuteOperation>());

                var srOriginateOps = TypeSubs.TryGetValue(Operations.SmartRollupOriginate, out var srOriginateSubs)
                    ? Repo.GetSmartRollupOriginateOps(new() { level = level }, new() { limit = -1 }, symbols, MichelineFormat.Raw)
                    : Task.FromResult(Enumerable.Empty<Models.SmartRollupOriginateOperation>());

                var srPublishOps = TypeSubs.TryGetValue(Operations.SmartRollupPublish, out var srPublishSubs)
                    ? Repo.GetSmartRollupPublishOps(new() { level = level }, new() { limit = -1 }, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.SmartRollupPublishOperation>());

                var srRecoverBondOps = TypeSubs.TryGetValue(Operations.SmartRollupRecoverBond, out var srRecoverBondSubs)
                    ? Repo.GetSmartRollupRecoverBondOps(new() { level = level }, new() { limit = -1 }, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.SmartRollupRecoverBondOperation>());

                var srRefuteOps = TypeSubs.TryGetValue(Operations.SmartRollupRefute, out var srRefuteSubs)
                    ? Repo.GetSmartRollupRefuteOps(new() { level = level }, new() { limit = -1 }, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.SmartRollupRefuteOperation>());

                var stakingOps = TypeSubs.TryGetValue(Operations.Staking, out var stakingSubs)
                    ? Repo.GetStakingOps(new() { level = level }, new() { limit = -1 }, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.StakingOperation>());

                var setDelegateParametersOps = TypeSubs.TryGetValue(Operations.SetDelegateParameters, out var setDelegateParametersSubs)
                    ? Repo.GetSetDelegateParametersOps(new() { level = level }, new() { limit = -1 }, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.SetDelegateParametersOperation>());

                var dalPublishCommitmentOps = TypeSubs.TryGetValue(Operations.DalPublishCommitment, out var dalPublishCommitmentSubs)
                    ? Repo.GetDalPublishCommitmentOps(new() { level = level }, new() { limit = -1 }, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.DalPublishCommitmentOperation>());

                var migrations = TypeSubs.TryGetValue(Operations.Migrations, out var migrationsSub)
                    ? Repo.GetMigrations(null, null, null, null, null, level, null, null, null, limit, MichelineFormat.Json, symbols, true, true)
                    : Task.FromResult(Enumerable.Empty<Models.MigrationOperation>());

                var penalties = TypeSubs.TryGetValue(Operations.RevelationPenalty, out var penaltiesSub)
                    ? Repo.GetRevelationPenalties(null, null, null, level, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.RevelationPenaltyOperation>());

                var baking = TypeSubs.TryGetValue(Operations.Baking, out var bakingSub)
                    ? Repo.GetBakings(null, null, null, null, null, level, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.BakingOperation>());

                var endorsingRewards = TypeSubs.TryGetValue(Operations.EndorsingRewards, out var endorsingRewardsSub)
                    ? Repo.GetEndorsingRewards(null, null, null, level, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.EndorsingRewardOperation>());

                var dalAttestationRewards = TypeSubs.TryGetValue(Operations.DalAttestationReward, out var dalAttestationRewardsSub)
                    ? Repo.GetDalAttestationRewards(null, null, null, level, null, null, null, limit, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.DalAttestationRewardOperation>());

                var autostakingOps = TypeSubs.TryGetValue(Operations.Autostaking, out var autostakingOpsSub)
                    ? Repo.GetAutostakingOps(new() { level = level }, new() { limit = -1 }, symbols)
                    : Task.FromResult(Enumerable.Empty<Models.AutostakingOperation>());

                await Task.WhenAll(
                    endorsements,
                    preendorsements,
                    proposals,
                    ballots,
                    activations,
                    dalEntrapmentEvidences,
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
                    updateConsensusKeyOps,
                    drainDelegateOps,
                    srAddMessagesOps,
                    srCementOps,
                    srExecuteOps,
                    srOriginateOps,
                    srPublishOps,
                    srRecoverBondOps,
                    srRefuteOps,
                    stakingOps,
                    setDelegateParametersOps,
                    dalPublishCommitmentOps,
                    migrations,
                    penalties,
                    baking,
                    endorsingRewards,
                    dalAttestationRewards,
                    autostakingOps);
                #endregion

                #region prepare to send
                var toSend = new Dictionary<string, List<Operation>>();
                
                void Add(HashSet<string> subs, Operation operation)
                {
                    foreach (var clientId in subs)
                    {
                        if (!toSend.TryGetValue(clientId, out var list))
                        {
                            list = [];
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
                            list = [];
                            toSend.Add(clientId, list);
                        }
                        list.AddRange(operations);
                    }
                }

                if (endorsements.Result.Any())
                {
                    if (endorsementsSub!.Subs != null)
                        AddRange(endorsementsSub.Subs, endorsements.Result);

                    if (endorsementsSub.AddressSubs != null)
                        foreach (var op in endorsements.Result)
                            if (endorsementsSub.AddressSubs.TryGetValue(op.Delegate.Address, out var delegateSubs) && delegateSubs.Subs != null)
                                Add(delegateSubs.Subs, op);
                }

                if (preendorsements.Result.Any())
                {
                    if (preendorsementsSub!.Subs != null)
                        AddRange(preendorsementsSub.Subs, preendorsements.Result);

                    if (preendorsementsSub.AddressSubs != null)
                        foreach (var op in preendorsements.Result)
                            if (preendorsementsSub.AddressSubs.TryGetValue(op.Delegate.Address, out var delegateSubs) && delegateSubs.Subs != null)
                                Add(delegateSubs.Subs, op);
                }

                if (ballots.Result.Any())
                {
                    if (ballotsSub!.Subs != null)
                        AddRange(ballotsSub.Subs, ballots.Result);

                    if (ballotsSub.AddressSubs != null)
                        foreach (var op in ballots.Result)
                            if (ballotsSub.AddressSubs.TryGetValue(op.Delegate.Address, out var delegateSubs) && delegateSubs.Subs != null)
                                Add(delegateSubs.Subs, op);
                }

                if (proposals.Result.Any())
                {
                    if (proposalsSub!.Subs != null)
                        AddRange(proposalsSub.Subs, proposals.Result);

                    if (proposalsSub.AddressSubs != null)
                        foreach (var op in proposals.Result)
                            if (proposalsSub.AddressSubs.TryGetValue(op.Delegate.Address, out var delegateSubs) && delegateSubs.Subs != null)
                                Add(delegateSubs.Subs, op);
                }

                if (activations.Result.Any())
                {
                    if (activationsSub!.Subs != null)
                        AddRange(activationsSub.Subs, activations.Result);

                    if (activationsSub.AddressSubs != null)
                        foreach (var op in activations.Result)
                            if (activationsSub.AddressSubs.TryGetValue(op.Account.Address, out var accountSubs) && accountSubs.Subs != null)
                                Add(accountSubs.Subs, op);
                }

                if (dalEntrapmentEvidences.Result.Any())
                {
                    if (dalEntrapmentEvidencesSub!.Subs != null)
                        AddRange(dalEntrapmentEvidencesSub.Subs, dalEntrapmentEvidences.Result);

                    if (dalEntrapmentEvidencesSub.AddressSubs != null)
                        foreach (var op in dalEntrapmentEvidences.Result)
                        {
                            if (dalEntrapmentEvidencesSub.AddressSubs.TryGetValue(op.Accuser.Address, out var accuserSubs) && accuserSubs.Subs != null)
                                Add(accuserSubs.Subs, op);

                            if (dalEntrapmentEvidencesSub.AddressSubs.TryGetValue(op.Offender.Address, out var offenderSubs) && offenderSubs.Subs != null)
                                Add(offenderSubs.Subs, op);
                        }
                }

                if (doubleBaking.Result.Any())
                {
                    if (doubleBakingSub!.Subs != null)
                        AddRange(doubleBakingSub.Subs, doubleBaking.Result);

                    if (doubleBakingSub.AddressSubs != null)
                        foreach (var op in doubleBaking.Result)
                        {
                            if (doubleBakingSub.AddressSubs.TryGetValue(op.Accuser.Address, out var accuserSubs) && accuserSubs.Subs != null)
                                Add(accuserSubs.Subs, op);

                            if (doubleBakingSub.AddressSubs.TryGetValue(op.Offender.Address, out var offenderSubs) && offenderSubs.Subs != null)
                                Add(offenderSubs.Subs, op);
                        }
                }

                if (doubleEndorsing.Result.Any())
                {
                    if (doubleEndorsingSub!.Subs != null)
                        AddRange(doubleEndorsingSub.Subs, doubleEndorsing.Result);

                    if (doubleEndorsingSub.AddressSubs != null)
                        foreach (var op in doubleEndorsing.Result)
                        {
                            if (doubleEndorsingSub.AddressSubs.TryGetValue(op.Accuser.Address, out var accuserSubs) && accuserSubs.Subs != null)
                                Add(accuserSubs.Subs, op);

                            if (doubleEndorsingSub.AddressSubs.TryGetValue(op.Offender.Address, out var offenderSubs) && offenderSubs.Subs != null)
                                Add(offenderSubs.Subs, op);
                        }
                }

                if (doublePreendorsing.Result.Any())
                {
                    if (doublePreendorsingSub!.Subs != null)
                        AddRange(doublePreendorsingSub.Subs, doublePreendorsing.Result);

                    if (doublePreendorsingSub.AddressSubs != null)
                        foreach (var op in doublePreendorsing.Result)
                        {
                            if (doublePreendorsingSub.AddressSubs.TryGetValue(op.Accuser.Address, out var accuserSubs) && accuserSubs.Subs != null)
                                Add(accuserSubs.Subs, op);

                            if (doublePreendorsingSub.AddressSubs.TryGetValue(op.Offender.Address, out var offenderSubs) && offenderSubs.Subs != null)
                                Add(offenderSubs.Subs, op);
                        }
                }

                if (revelations.Result.Any())
                {
                    if (revelationsSub!.Subs != null)
                        AddRange(revelationsSub.Subs, revelations.Result);

                    if (revelationsSub.AddressSubs != null)
                        foreach (var op in revelations.Result)
                        {
                            if (revelationsSub.AddressSubs.TryGetValue(op.Baker.Address, out var bakerSubs) && bakerSubs.Subs != null)
                                Add(bakerSubs.Subs, op);

                            if (revelationsSub.AddressSubs.TryGetValue(op.Sender.Address, out var senderSubs) && senderSubs.Subs != null)
                                Add(senderSubs.Subs, op);
                        }
                }

                if (vdfRevelations.Result.Any())
                {
                    if (vdfRevelationsSub!.Subs != null)
                        AddRange(vdfRevelationsSub.Subs, vdfRevelations.Result);

                    if (vdfRevelationsSub.AddressSubs != null)
                        foreach (var op in vdfRevelations.Result)
                            if (vdfRevelationsSub.AddressSubs.TryGetValue(op.Baker.Address, out var bakerSubs) && bakerSubs.Subs != null)
                                Add(bakerSubs.Subs, op);
                }

                if (delegations.Result.Any())
                {
                    if (delegationsSub!.Subs != null)
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
                    if (originationsSub!.Subs != null)
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
                    if (transactionsSub!.Subs != null)
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
                    if (revealsSub!.Subs != null)
                        AddRange(revealsSub.Subs, reveals.Result);

                    if (revealsSub.AddressSubs != null)
                        foreach (var op in reveals.Result)
                            if (revealsSub.AddressSubs.TryGetValue(op.Sender.Address, out var senderSubs) && senderSubs.Subs != null)
                                Add(senderSubs.Subs, op);
                }

                if (registerConstants.Result.Any())
                {
                    if (registerConstantsSub!.Subs != null)
                        AddRange(registerConstantsSub.Subs, registerConstants.Result);

                    if (registerConstantsSub.AddressSubs != null)
                        foreach (var op in registerConstants.Result)
                            if (registerConstantsSub.AddressSubs.TryGetValue(op.Sender.Address, out var senderSubs) && senderSubs.Subs != null)
                                Add(senderSubs.Subs, op);
                }

                if (setDepositsLimits.Result.Any())
                {
                    if (setDepositsLimitsSub!.Subs != null)
                        AddRange(setDepositsLimitsSub.Subs, setDepositsLimits.Result);

                    if (setDepositsLimitsSub.AddressSubs != null)
                        foreach (var op in setDepositsLimits.Result)
                            if (setDepositsLimitsSub.AddressSubs.TryGetValue(op.Sender.Address, out var senderSubs) && senderSubs.Subs != null)
                                Add(senderSubs.Subs, op);
                }

                if (transferTicketOps.Result.Any())
                {
                    if (transferTicketSub!.Subs != null)
                        AddRange(transferTicketSub.Subs, transferTicketOps.Result);

                    if (transferTicketSub.AddressSubs != null)
                        foreach (var op in transferTicketOps.Result)
                        {
                            if (transferTicketSub.AddressSubs.TryGetValue(op.Sender.Address, out var senderSubs) && senderSubs.Subs != null)
                                Add(senderSubs.Subs, op);

                            if (op.Target != null && transferTicketSub.AddressSubs.TryGetValue(op.Target.Address, out var targetSubs) && targetSubs.Subs != null)
                                Add(targetSubs.Subs, op);

                            if (op.Ticketer != null && transferTicketSub.AddressSubs.TryGetValue(op.Ticketer.Address, out var ticketerSubs) && ticketerSubs.Subs != null)
                                Add(ticketerSubs.Subs, op);
                        }
                }

                if (txRollupCommitOps.Result.Any())
                {
                    if (txRollupCommitSub!.Subs != null)
                        AddRange(txRollupCommitSub.Subs, txRollupCommitOps.Result);

                    if (txRollupCommitSub.AddressSubs != null)
                        foreach (var op in txRollupCommitOps.Result)
                        {
                            if (txRollupCommitSub.AddressSubs.TryGetValue(op.Sender.Address, out var senderSubs) && senderSubs.Subs != null)
                                Add(senderSubs.Subs, op);

                            if (op.Rollup != null && txRollupCommitSub.AddressSubs.TryGetValue(op.Rollup.Address, out var rollupSubs) && rollupSubs.Subs != null)
                                Add(rollupSubs.Subs, op);
                        }
                }

                if (txRollupDispatchTicketsOps.Result.Any())
                {
                    if (txRollupDispatchTicketsSub!.Subs != null)
                        AddRange(txRollupDispatchTicketsSub.Subs, txRollupDispatchTicketsOps.Result);

                    if (txRollupDispatchTicketsSub.AddressSubs != null)
                        foreach (var op in txRollupDispatchTicketsOps.Result)
                        {
                            if (txRollupDispatchTicketsSub.AddressSubs.TryGetValue(op.Sender.Address, out var senderSubs) && senderSubs.Subs != null)
                                Add(senderSubs.Subs, op);

                            if (op.Rollup != null && txRollupDispatchTicketsSub.AddressSubs.TryGetValue(op.Rollup.Address, out var rollupSubs) && rollupSubs.Subs != null)
                                Add(rollupSubs.Subs, op);
                        }
                }

                if (txRollupFinalizeCommitmentOps.Result.Any())
                {
                    if (txRollupFinalizeCommitmentSub!.Subs != null)
                        AddRange(txRollupFinalizeCommitmentSub.Subs, txRollupFinalizeCommitmentOps.Result);

                    if (txRollupFinalizeCommitmentSub.AddressSubs != null)
                        foreach (var op in txRollupFinalizeCommitmentOps.Result)
                        {
                            if (txRollupFinalizeCommitmentSub.AddressSubs.TryGetValue(op.Sender.Address, out var senderSubs) && senderSubs.Subs != null)
                                Add(senderSubs.Subs, op);

                            if (op.Rollup != null && txRollupFinalizeCommitmentSub.AddressSubs.TryGetValue(op.Rollup.Address, out var rollupSubs) && rollupSubs.Subs != null)
                                Add(rollupSubs.Subs, op);
                        }
                }

                if (txRollupOriginationOps.Result.Any())
                {
                    if (txRollupOriginationSub!.Subs != null)
                        AddRange(txRollupOriginationSub.Subs, txRollupOriginationOps.Result);

                    if (txRollupOriginationSub.AddressSubs != null)
                        foreach (var op in txRollupOriginationOps.Result)
                        {
                            if (txRollupOriginationSub.AddressSubs.TryGetValue(op.Sender.Address, out var senderSubs) && senderSubs.Subs != null)
                                Add(senderSubs.Subs, op);

                            if (op.Rollup != null && txRollupOriginationSub.AddressSubs.TryGetValue(op.Rollup.Address, out var rollupSubs) && rollupSubs.Subs != null)
                                Add(rollupSubs.Subs, op);
                        }
                }

                if (txRollupRejectionOps.Result.Any())
                {
                    if (txRollupRejectionSub!.Subs != null)
                        AddRange(txRollupRejectionSub.Subs, txRollupRejectionOps.Result);

                    if (txRollupRejectionSub.AddressSubs != null)
                        foreach (var op in txRollupRejectionOps.Result)
                        {
                            if (txRollupRejectionSub.AddressSubs.TryGetValue(op.Sender.Address, out var senderSubs) && senderSubs.Subs != null)
                                Add(senderSubs.Subs, op);

                            if (op.Committer != null && txRollupRejectionSub.AddressSubs.TryGetValue(op.Committer.Address, out var committerSubs) && committerSubs.Subs != null)
                                Add(committerSubs.Subs, op);

                            if (op.Rollup != null && txRollupRejectionSub.AddressSubs.TryGetValue(op.Rollup.Address, out var rollupSubs) && rollupSubs.Subs != null)
                                Add(rollupSubs.Subs, op);
                        }
                }

                if (txRollupRemoveCommitmentOps.Result.Any())
                {
                    if (txRollupRemoveCommitmentSub!.Subs != null)
                        AddRange(txRollupRemoveCommitmentSub.Subs, txRollupRemoveCommitmentOps.Result);

                    if (txRollupRemoveCommitmentSub.AddressSubs != null)
                        foreach (var op in txRollupRemoveCommitmentOps.Result)
                        {
                            if (txRollupRemoveCommitmentSub.AddressSubs.TryGetValue(op.Sender.Address, out var senderSubs) && senderSubs.Subs != null)
                                Add(senderSubs.Subs, op);

                            if (op.Rollup != null && txRollupRemoveCommitmentSub.AddressSubs.TryGetValue(op.Rollup.Address, out var rollupSubs) && rollupSubs.Subs != null)
                                Add(rollupSubs.Subs, op);
                        }
                }

                if (txRollupReturnBondOps.Result.Any())
                {
                    if (txRollupReturnBondSub!.Subs != null)
                        AddRange(txRollupReturnBondSub.Subs, txRollupReturnBondOps.Result);

                    if (txRollupReturnBondSub.AddressSubs != null)
                        foreach (var op in txRollupReturnBondOps.Result)
                        {
                            if (txRollupReturnBondSub.AddressSubs.TryGetValue(op.Sender.Address, out var senderSubs) && senderSubs.Subs != null)
                                Add(senderSubs.Subs, op);

                            if (op.Rollup != null && txRollupReturnBondSub.AddressSubs.TryGetValue(op.Rollup.Address, out var rollupSubs) && rollupSubs.Subs != null)
                                Add(rollupSubs.Subs, op);
                        }
                }

                if (txRollupSubmitBatchOps.Result.Any())
                {
                    if (txRollupSubmitBatchSub!.Subs != null)
                        AddRange(txRollupSubmitBatchSub.Subs, txRollupSubmitBatchOps.Result);

                    if (txRollupSubmitBatchSub.AddressSubs != null)
                        foreach (var op in txRollupSubmitBatchOps.Result)
                        {
                            if (txRollupSubmitBatchSub.AddressSubs.TryGetValue(op.Sender.Address, out var senderSubs) && senderSubs.Subs != null)
                                Add(senderSubs.Subs, op);

                            if (op.Rollup != null && txRollupSubmitBatchSub.AddressSubs.TryGetValue(op.Rollup.Address, out var rollupSubs) && rollupSubs.Subs != null)
                                Add(rollupSubs.Subs, op);
                        }
                }

                if (increasePaidStorageOps.Result.Any())
                {
                    if (increasePaidStorageSubs!.Subs != null)
                        AddRange(increasePaidStorageSubs.Subs, increasePaidStorageOps.Result);

                    if (increasePaidStorageSubs.AddressSubs != null)
                        foreach (var op in increasePaidStorageOps.Result)
                        {
                            if (increasePaidStorageSubs.AddressSubs.TryGetValue(op.Sender.Address, out var senderSubs) && senderSubs.Subs != null)
                                Add(senderSubs.Subs, op);

                            if (op.Contract != null && increasePaidStorageSubs.AddressSubs.TryGetValue(op.Contract.Address, out var contractSubs) && contractSubs.Subs != null)
                                Add(contractSubs.Subs, op);
                        }
                }

                if (updateConsensusKeyOps.Result.Any())
                {
                    if (updateConsensusKeySubs!.Subs != null)
                        AddRange(updateConsensusKeySubs.Subs, updateConsensusKeyOps.Result);

                    if (updateConsensusKeySubs.AddressSubs != null)
                        foreach (var op in updateConsensusKeyOps.Result)
                        {
                            if (updateConsensusKeySubs.AddressSubs.TryGetValue(op.Sender.Address, out var senderSubs) && senderSubs.Subs != null)
                                Add(senderSubs.Subs, op);
                        }
                }

                if (drainDelegateOps.Result.Any())
                {
                    if (drainDelegateSubs!.Subs != null)
                        AddRange(drainDelegateSubs.Subs, drainDelegateOps.Result);

                    if (drainDelegateSubs.AddressSubs != null)
                        foreach (var op in drainDelegateOps.Result)
                        {
                            if (drainDelegateSubs.AddressSubs.TryGetValue(op.Delegate.Address, out var delegateSubs) && delegateSubs.Subs != null)
                                Add(delegateSubs.Subs, op);

                            if (drainDelegateSubs.AddressSubs.TryGetValue(op.Target.Address, out var targetSubs) && targetSubs.Subs != null)
                                Add(targetSubs.Subs, op);
                        }
                }

                if (srAddMessagesOps.Result.Any())
                {
                    if (srAddMessagesSubs!.Subs != null)
                        AddRange(srAddMessagesSubs.Subs, srAddMessagesOps.Result);

                    if (srAddMessagesSubs.AddressSubs != null)
                        foreach (var op in srAddMessagesOps.Result)
                        {
                            if (srAddMessagesSubs.AddressSubs.TryGetValue(op.Sender.Address, out var senderSubs) && senderSubs.Subs != null)
                                Add(senderSubs.Subs, op);
                        }
                }

                if (srCementOps.Result.Any())
                {
                    if (srCementSubs!.Subs != null)
                        AddRange(srCementSubs.Subs, srCementOps.Result);

                    if (srCementSubs.AddressSubs != null)
                        foreach (var op in srCementOps.Result)
                        {
                            if (srCementSubs.AddressSubs.TryGetValue(op.Sender.Address, out var senderSubs) && senderSubs.Subs != null)
                                Add(senderSubs.Subs, op);

                            if (op.Rollup != null && srCementSubs.AddressSubs.TryGetValue(op.Rollup.Address, out var rollupSubs) && rollupSubs.Subs != null)
                                Add(rollupSubs.Subs, op);
                        }
                }

                if (srExecuteOps.Result.Any())
                {
                    if (srExecuteSubs!.Subs != null)
                        AddRange(srExecuteSubs.Subs, srExecuteOps.Result);

                    if (srExecuteSubs.AddressSubs != null)
                        foreach (var op in srExecuteOps.Result)
                        {
                            if (srExecuteSubs.AddressSubs.TryGetValue(op.Sender.Address, out var senderSubs) && senderSubs.Subs != null)
                                Add(senderSubs.Subs, op);

                            if (op.Rollup != null && srExecuteSubs.AddressSubs.TryGetValue(op.Rollup.Address, out var rollupSubs) && rollupSubs.Subs != null)
                                Add(rollupSubs.Subs, op);
                        }
                }

                if (srOriginateOps.Result.Any())
                {
                    if (srOriginateSubs!.Subs != null)
                        AddRange(srOriginateSubs.Subs, srOriginateOps.Result);

                    if (srOriginateSubs.AddressSubs != null)
                        foreach (var op in srOriginateOps.Result)
                        {
                            if (srOriginateSubs.AddressSubs.TryGetValue(op.Sender.Address, out var senderSubs) && senderSubs.Subs != null)
                                Add(senderSubs.Subs, op);

                            if (op.Rollup != null && srOriginateSubs.AddressSubs.TryGetValue(op.Rollup.Address, out var rollupSubs) && rollupSubs.Subs != null)
                                Add(rollupSubs.Subs, op);
                        }
                }

                if (srPublishOps.Result.Any())
                {
                    if (srPublishSubs!.Subs != null)
                        AddRange(srPublishSubs.Subs, srPublishOps.Result);

                    if (srPublishSubs.AddressSubs != null)
                        foreach (var op in srPublishOps.Result)
                        {
                            if (srPublishSubs.AddressSubs.TryGetValue(op.Sender.Address, out var senderSubs) && senderSubs.Subs != null)
                                Add(senderSubs.Subs, op);

                            if (op.Rollup != null && srPublishSubs.AddressSubs.TryGetValue(op.Rollup.Address, out var rollupSubs) && rollupSubs.Subs != null)
                                Add(rollupSubs.Subs, op);
                        }
                }

                if (srRecoverBondOps.Result.Any())
                {
                    if (srRecoverBondSubs!.Subs != null)
                        AddRange(srRecoverBondSubs.Subs, srRecoverBondOps.Result);

                    if (srRecoverBondSubs.AddressSubs != null)
                        foreach (var op in srRecoverBondOps.Result)
                        {
                            if (srRecoverBondSubs.AddressSubs.TryGetValue(op.Sender.Address, out var senderSubs) && senderSubs.Subs != null)
                                Add(senderSubs.Subs, op);

                            if (op.Rollup != null && srRecoverBondSubs.AddressSubs.TryGetValue(op.Rollup.Address, out var rollupSubs) && rollupSubs.Subs != null)
                                Add(rollupSubs.Subs, op);

                            if (op.Staker != null && srRecoverBondSubs.AddressSubs.TryGetValue(op.Staker.Address, out var stakerSubs) && stakerSubs.Subs != null)
                                Add(stakerSubs.Subs, op);
                        }
                }

                if (srRefuteOps.Result.Any())
                {
                    if (srRefuteSubs!.Subs != null)
                        AddRange(srRefuteSubs.Subs, srRefuteOps.Result);

                    if (srRefuteSubs.AddressSubs != null)
                        foreach (var op in srRefuteOps.Result)
                        {
                            if (srRefuteSubs.AddressSubs.TryGetValue(op.Sender.Address, out var senderSubs) && senderSubs.Subs != null)
                                Add(senderSubs.Subs, op);

                            if (op.Rollup != null && srRefuteSubs.AddressSubs.TryGetValue(op.Rollup.Address, out var rollupSubs) && rollupSubs.Subs != null)
                                Add(rollupSubs.Subs, op);

                            if (op.Game != null && srRefuteSubs.AddressSubs.TryGetValue(op.Game.Initiator.Address, out var initiatorSubs) && initiatorSubs.Subs != null)
                                Add(initiatorSubs.Subs, op);

                            if (op.Game != null && srRefuteSubs.AddressSubs.TryGetValue(op.Game.Opponent.Address, out var opponentSubs) && opponentSubs.Subs != null)
                                Add(opponentSubs.Subs, op);
                        }
                }

                if (stakingOps.Result.Any())
                {
                    if (stakingSubs!.Subs != null)
                        AddRange(stakingSubs.Subs, stakingOps.Result);

                    if (stakingSubs.AddressSubs != null)
                        foreach (var op in stakingOps.Result)
                        {
                            if (stakingSubs.AddressSubs.TryGetValue(op.Sender.Address, out var senderSubs) && senderSubs.Subs != null)
                                Add(senderSubs.Subs, op);

                            if (op.Baker != null && stakingSubs.AddressSubs.TryGetValue(op.Baker.Address, out var bakerSubs) && bakerSubs.Subs != null)
                                Add(bakerSubs.Subs, op);
                        }
                }

                if (setDelegateParametersOps.Result.Any())
                {
                    if (setDelegateParametersSubs!.Subs != null)
                        AddRange(setDelegateParametersSubs.Subs, setDelegateParametersOps.Result);

                    if (setDelegateParametersSubs.AddressSubs != null)
                        foreach (var op in setDelegateParametersOps.Result)
                            if (setDelegateParametersSubs.AddressSubs.TryGetValue(op.Sender.Address, out var senderSubs) && senderSubs.Subs != null)
                                Add(senderSubs.Subs, op);
                }

                if (dalPublishCommitmentOps.Result.Any())
                {
                    if (dalPublishCommitmentSubs!.Subs != null)
                        AddRange(dalPublishCommitmentSubs.Subs, dalPublishCommitmentOps.Result);

                    if (dalPublishCommitmentSubs.AddressSubs != null)
                        foreach (var op in dalPublishCommitmentOps.Result)
                            if (dalPublishCommitmentSubs.AddressSubs.TryGetValue(op.Sender.Address, out var senderSubs) && senderSubs.Subs != null)
                                Add(senderSubs.Subs, op);
                }

                if (migrations.Result.Any())
                {
                    if (migrationsSub!.Subs != null)
                        AddRange(migrationsSub.Subs, migrations.Result);

                    if (migrationsSub.AddressSubs != null)
                        foreach (var op in migrations.Result)
                            if (migrationsSub.AddressSubs.TryGetValue(op.Account.Address, out var accountSubs) && accountSubs.Subs != null)
                                Add(accountSubs.Subs, op);
                }

                if (penalties.Result.Any())
                {
                    if (penaltiesSub!.Subs != null)
                        AddRange(penaltiesSub.Subs, penalties.Result);

                    if (penaltiesSub.AddressSubs != null)
                        foreach (var op in penalties.Result)
                            if (penaltiesSub.AddressSubs.TryGetValue(op.Baker.Address, out var bakerSubs) && bakerSubs.Subs != null)
                                Add(bakerSubs.Subs, op);
                }

                if (baking.Result.Any())
                {
                    if (bakingSub!.Subs != null)
                        AddRange(bakingSub.Subs, baking.Result);

                    if (bakingSub.AddressSubs != null)
                        foreach (var op in baking.Result)
                        {
                            if (bakingSub.AddressSubs.TryGetValue(op.Proposer.Address, out var proposerSubs) && proposerSubs.Subs != null)
                                Add(proposerSubs.Subs, op);

                            if (bakingSub.AddressSubs.TryGetValue(op.Producer.Address, out var producerSubs) && producerSubs.Subs != null)
                                Add(producerSubs.Subs, op);
                        }
                }

                if (endorsingRewards.Result.Any())
                {
                    if (endorsingRewardsSub!.Subs != null)
                        AddRange(endorsingRewardsSub.Subs, endorsingRewards.Result);

                    if (endorsingRewardsSub.AddressSubs != null)
                        foreach (var op in endorsingRewards.Result)
                            if (endorsingRewardsSub.AddressSubs.TryGetValue(op.Baker.Address, out var bakerSubs) && bakerSubs.Subs != null)
                                Add(bakerSubs.Subs, op);
                }

                if (dalAttestationRewards.Result.Any())
                {
                    if (dalAttestationRewardsSub!.Subs != null)
                        AddRange(dalAttestationRewardsSub.Subs, dalAttestationRewards.Result);

                    if (dalAttestationRewardsSub.AddressSubs != null)
                        foreach (var op in dalAttestationRewards.Result)
                            if (dalAttestationRewardsSub.AddressSubs.TryGetValue(op.Baker.Address, out var bakerSubs) && bakerSubs.Subs != null)
                                Add(bakerSubs.Subs, op);
                }

                if (autostakingOps.Result.Any())
                {
                    if (autostakingOpsSub!.Subs != null)
                        AddRange(autostakingOpsSub.Subs, autostakingOps.Result);

                    if (autostakingOpsSub.AddressSubs != null)
                        foreach (var op in autostakingOps.Result)
                            if (autostakingOpsSub.AddressSubs.TryGetValue(op.Baker.Address, out var bakerSubs) && bakerSubs.Subs != null)
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

                    Logger.LogDebug("{cnt} operations sent to {id}", operations.Count, connectionId);
                }
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
                    Logger.LogError(ex, "Sendings failed");
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
                        typeSub.AddressSubs ??= [];
                        if (!typeSub.AddressSubs.TryGetValue(parameter.Address, out var addressSub))
                        {
                            addressSub = new();
                            typeSub.AddressSubs.Add(parameter.Address, addressSub);
                        }
                        if (parameter.CodeHash != null)
                        {
                            addressSub.CodeHashSubs ??= [];
                            TryAdd(addressSub.CodeHashSubs, (int)parameter.CodeHash, connectionId);
                        }
                        else
                        {
                            addressSub.Subs ??= [];
                            TryAdd(addressSub.Subs, connectionId);
                        }
                    }
                    else if (parameter.CodeHash != null)
                    {
                        typeSub.CodeHashSubs ??= [];
                        TryAdd(typeSub.CodeHashSubs, (int)parameter.CodeHash, connectionId);
                    }
                    else
                    {
                        typeSub.Subs ??= [];
                        TryAdd(typeSub.Subs, connectionId);
                    }
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
                    Logger.LogError(ex, "Sending failed");
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

        private static IEnumerable<Operation> Distinct(List<Operation> ops)
        {
            var set = new HashSet<long>(ops.Count);
            foreach (var op in ops)
                if (set.Add(op.Id))
                    yield return op;
        }
    }
}