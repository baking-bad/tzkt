﻿using System.Text.Json;
using App.Metrics;
using Tzkt.Data;
using Tzkt.Data.Models;
using Tzkt.Sync.Services;
using Tzkt.Sync.Protocols.Proto17;

namespace Tzkt.Sync.Protocols
{
    class Proto17Handler : ProtocolHandler
    {
        public override IDiagnostics Diagnostics { get; }
        public override IValidator Validator { get; }
        public override IRpc Rpc { get; }
        public override string VersionName => "nairobi_017";
        public override int VersionNumber => 17;

        public Proto17Handler(TezosNode node, TzktContext db, CacheService cache, QuotesService quotes, IServiceProvider services, IConfiguration config, ILogger<Proto17Handler> logger, IMetrics metrics)
            : base(node, db, cache, quotes, services, config, logger, metrics)
        {
            Rpc = new Rpc(node);
            Diagnostics = new Diagnostics(this);
            Validator = new Validator(this);
        }

        public override Task Activate(AppState state, JsonElement block) => new ProtoActivator(this).Activate(state, block);
        public override Task Deactivate(AppState state) => new ProtoActivator(this).Deactivate(state);

        public override async Task Commit(JsonElement block)
        {
            await new StatisticsCommit(this).Apply(block);

            var blockCommit = new BlockCommit(this);
            await blockCommit.Apply(block);

            var cycleCommit = new CycleCommit(this);
            await cycleCommit.Apply(blockCommit.Block);

            await new SoftwareCommit(this).Apply(blockCommit.Block, block);
            await new DeactivationCommit(this).Apply(blockCommit.Block, block);

            #region implicit operations
            foreach (var op in block
                .Required("metadata")
                .RequiredArray("implicit_operations_results")
                .EnumerateArray()
                .Where(x => x.RequiredString("kind") == "transaction"))
                await new SubsidyCommit(this).Apply(blockCommit.Block, op);
            #endregion

            var operations = block.RequiredArray("operations", 4);

            #region operations 0
            foreach (var operation in operations[0].EnumerateArray())
            {
                foreach (var content in operation.RequiredArray("contents", 1).EnumerateArray())
                {
                    switch (content.RequiredString("kind"))
                    {
                        case "endorsement":
                            await new EndorsementsCommit(this).Apply(blockCommit.Block, operation, content);
                            break;
                        case "preendorsement":
                            new PreendorsementsCommit(this).Apply(blockCommit.Block, operation, content);
                            break;
                        default:
                            throw new NotImplementedException($"'{content.RequiredString("kind")}' is not allowed in operations[0]");
                    }
                }
            }
            #endregion

            #region operations 1
            var dictatorSeen = false;
            foreach (var operation in operations[1].EnumerateArray())
            {
                foreach (var content in operation.RequiredArray("contents", 1).EnumerateArray())
                {
                    switch (content.RequiredString("kind"))
                    {
                        case "proposals":
                            var proposalsCommit = new ProposalsCommit(this);
                            await proposalsCommit.Apply(blockCommit.Block, operation, content);
                            dictatorSeen = proposalsCommit.DictatorSeen;
                            break;
                        case "ballot":
                            await new BallotsCommit(this).Apply(blockCommit.Block, operation, content);
                            break;
                        default:
                            throw new NotImplementedException($"'{content.RequiredString("kind")}' is not allowed in operations[1]");
                    }
                }
                if (dictatorSeen) break;
            }
            #endregion

            #region operations 2
            foreach (var operation in operations[2].EnumerateArray())
            {
                foreach (var content in operation.RequiredArray("contents", 1).EnumerateArray())
                {
                    switch (content.RequiredString("kind"))
                    {
                        case "activate_account":
                            await new ActivationsCommit(this).Apply(blockCommit.Block, operation, content);
                            break;
                        case "double_baking_evidence":
                            new DoubleBakingCommit(this).Apply(blockCommit.Block, operation, content);
                            break;
                        case "double_endorsement_evidence":
                            new DoubleEndorsingCommit(this).Apply(blockCommit.Block, operation, content);
                            break;
                        case "double_preendorsement_evidence":
                            new DoublePreendorsingCommit(this).Apply(blockCommit.Block, operation, content);
                            break;
                        case "seed_nonce_revelation":
                            await new NonceRevelationsCommit(this).Apply(blockCommit.Block, operation, content);
                            break;
                        case "vdf_revelation":
                            await new VdfRevelationCommit(this).Apply(blockCommit.Block, operation, content);
                            break;
                        case "drain_delegate":
                            await new DrainDelegateCommit(this).Apply(blockCommit.Block, operation, content);
                            break;
                        default:
                            throw new NotImplementedException($"'{content.RequiredString("kind")}' is not allowed in operations[2]");
                    }
                }
            }
            #endregion

            var bigMapCommit = new BigMapCommit(this);
            var ticketsCommit = new TicketsCommit(this);

            #region operations 3
            foreach (var operation in operations[3].EnumerateArray())
            {
                Manager.Init(operation);
                foreach (var content in operation.RequiredArray("contents").EnumerateArray())
                {
                    switch (content.RequiredString("kind"))
                    {
                        case "set_deposits_limit":
                            await new SetDepositsLimitCommit(this).Apply(blockCommit.Block, operation, content);
                            break;
                        case "increase_paid_storage":
                            await new IncreasePaidStorageCommit(this).Apply(blockCommit.Block, operation, content);
                            break;
                        case "update_consensus_key":
                            await new UpdateConsensusKeyCommit(this).Apply(blockCommit.Block, operation, content);
                            break;
                        case "reveal":
                            await new RevealsCommit(this).Apply(blockCommit.Block, operation, content);
                            break;
                        case "register_global_constant":
                            await new RegisterConstantsCommit(this).Apply(blockCommit.Block, operation, content);
                            break;
                        case "delegation":
                            await new DelegationsCommit(this).Apply(blockCommit.Block, operation, content);
                            break;
                        case "origination":
                            var orig = new OriginationsCommit(this);
                            await orig.Apply(blockCommit.Block, operation, content);
                            if (orig.BigMapDiffs != null)
                                bigMapCommit.Append(orig.Origination, orig.Contract!, orig.BigMapDiffs);
                            break;
                        case "transaction":
                            var parent = new TransactionsCommit(this);
                            await parent.Apply(blockCommit.Block, operation, content);
                            if (parent.BigMapDiffs != null)
                                bigMapCommit.Append(parent.Transaction, (parent.Target as Contract)!, parent.BigMapDiffs);
                            if (parent.TicketUpdates != null)
                                ticketsCommit.Append(parent.Transaction, parent.Transaction, parent.TicketUpdates);

                            if (content.Required("metadata").TryGetProperty("internal_operation_results", out var internalResult))
                            {
                                foreach (var internalContent in internalResult.EnumerateArray())
                                {
                                    switch (internalContent.RequiredString("kind"))
                                    {
                                        case "delegation":
                                            await new DelegationsCommit(this).ApplyInternal(blockCommit.Block, parent.Transaction, internalContent);
                                            break;
                                        case "origination":
                                            var internalOrig = new OriginationsCommit(this);
                                            await internalOrig.ApplyInternal(blockCommit.Block, parent.Transaction, internalContent);
                                            if (internalOrig.BigMapDiffs != null)
                                                bigMapCommit.Append(internalOrig.Origination, internalOrig.Contract!, internalOrig.BigMapDiffs);
                                            break;
                                        case "transaction":
                                            var internalTx = new TransactionsCommit(this);
                                            await internalTx.ApplyInternal(blockCommit.Block, parent.Transaction, internalContent);
                                            if (internalTx.BigMapDiffs != null)
                                                bigMapCommit.Append(internalTx.Transaction, (internalTx.Target as Contract)!, internalTx.BigMapDiffs);
                                            if (internalTx.TicketUpdates != null)
                                                ticketsCommit.Append(parent.Transaction, internalTx.Transaction, internalTx.TicketUpdates);
                                            break;
                                        case "event":
                                            await new ContractEventCommit(this).Apply(blockCommit.Block, internalContent);
                                            break;
                                        default:
                                            throw new NotImplementedException($"internal '{internalContent.RequiredString("kind")}' is not implemented");
                                    }
                                }
                            }
                            break;
                        case "tx_rollup_origination":
                            await new TxRollupOriginationCommit(this).Apply(blockCommit.Block, operation, content);
                            break;
                        case "tx_rollup_submit_batch":
                            await new TxRollupSubmitBatchCommit(this).Apply(blockCommit.Block, operation, content);
                            break;
                        case "tx_rollup_commit":
                            await new TxRollupCommitCommit(this).Apply(blockCommit.Block, operation, content);
                            break;
                        case "tx_rollup_finalize_commitment":
                            await new TxRollupFinalizeCommitmentCommit(this).Apply(blockCommit.Block, operation, content);
                            break;
                        case "tx_rollup_remove_commitment":
                            await new TxRollupRemoveCommitmentCommit(this).Apply(blockCommit.Block, operation, content);
                            break;
                        case "tx_rollup_return_bond":
                            await new TxRollupReturnBondCommit(this).Apply(blockCommit.Block, operation, content);
                            break;
                        case "tx_rollup_rejection":
                            await new TxRollupRejectionCommit(this).Apply(blockCommit.Block, operation, content);
                            break;
                        case "tx_rollup_dispatch_tickets":
                            await new TxRollupDispatchTicketsCommit(this).Apply(blockCommit.Block, operation, content);
                            break;
                        case "transfer_ticket":
                            var parent1 = new TransferTicketCommit(this);
                            await parent1.Apply(blockCommit.Block, operation, content);
                            if (parent1.TicketUpdates != null)
                                ticketsCommit.Append(parent1.Operation, parent1.Operation, parent1.TicketUpdates);
                            if (content.Required("metadata").TryGetProperty("internal_operation_results", out var internalResult1))
                            {
                                foreach (var internalContent in internalResult1.EnumerateArray())
                                {
                                    switch (internalContent.RequiredString("kind"))
                                    {
                                        case "transaction":
                                            var internalTx = new TransactionsCommit(this);
                                            await internalTx.ApplyInternal(blockCommit.Block, parent1.Operation, internalContent);
                                            if (internalTx.BigMapDiffs != null)
                                                bigMapCommit.Append(internalTx.Transaction, (internalTx.Target as Contract)!, internalTx.BigMapDiffs);
                                            if (internalTx.TicketUpdates != null)
                                                ticketsCommit.Append(parent1.Operation, internalTx.Transaction, internalTx.TicketUpdates);
                                            break;
                                        case "event":
                                            await new ContractEventCommit(this).Apply(blockCommit.Block, internalContent);
                                            break;
                                        default:
                                            throw new NotImplementedException($"internal '{internalContent.RequiredString("kind")}' inside 'transfer_ticket' is not expected");
                                    }
                                }
                            }
                            break;
                        case "smart_rollup_add_messages":
                            await new SmartRollupAddMessagesCommit(this).Apply(blockCommit.Block, operation, content);
                            break;
                        case "smart_rollup_cement":
                            await new SmartRollupCementCommit(this).Apply(blockCommit.Block, operation, content);
                            break;
                        case "smart_rollup_execute_outbox_message":
                            var parent2 = new SmartRollupExecuteCommit(this);
                            await parent2.Apply(blockCommit.Block, operation, content);
                            if (parent2.TicketUpdates != null)
                                ticketsCommit.Append(parent2.Operation, parent2.Operation, parent2.TicketUpdates);
                            if (content.Required("metadata").TryGetProperty("internal_operation_results", out var internalResult2))
                            {
                                foreach (var internalContent in internalResult2.EnumerateArray())
                                {
                                    switch (internalContent.RequiredString("kind"))
                                    {
                                        case "delegation":
                                            await new DelegationsCommit(this).ApplyInternal(blockCommit.Block, parent2.Operation, internalContent);
                                            break;
                                        case "origination":
                                            var internalOrig = new OriginationsCommit(this);
                                            await internalOrig.ApplyInternal(blockCommit.Block, parent2.Operation, internalContent);
                                            if (internalOrig.BigMapDiffs != null)
                                                bigMapCommit.Append(internalOrig.Origination, internalOrig.Contract!, internalOrig.BigMapDiffs);
                                            break;
                                        case "transaction":
                                            var internalTx = new TransactionsCommit(this);
                                            await internalTx.ApplyInternal(blockCommit.Block, parent2.Operation, internalContent);
                                            if (internalTx.BigMapDiffs != null)
                                                bigMapCommit.Append(internalTx.Transaction, (internalTx.Target as Contract)!, internalTx.BigMapDiffs);
                                            if (internalTx.TicketUpdates != null)
                                                ticketsCommit.Append(parent2.Operation, internalTx.Transaction, internalTx.TicketUpdates);
                                            break;
                                        case "event":
                                            await new ContractEventCommit(this).Apply(blockCommit.Block, internalContent);
                                            break;
                                        default:
                                            throw new NotImplementedException($"internal '{internalContent.RequiredString("kind")}' is not implemented");
                                    }
                                }
                            }
                            break;
                        case "smart_rollup_originate":
                            await new SmartRollupOriginateCommit(this).Apply(blockCommit.Block, operation, content);
                            break;
                        case "smart_rollup_publish":
                            await new SmartRollupPublishCommit(this).Apply(blockCommit.Block, operation, content);
                            break;
                        case "smart_rollup_recover_bond":
                            await new SmartRollupRecoverBondCommit(this).Apply(blockCommit.Block, operation, content);
                            break;
                        case "smart_rollup_refute":
                            await new SmartRollupRefuteCommit(this).Apply(blockCommit.Block, operation, content);
                            break;
                        case "smart_rollup_timeout":
                            await new SmartRollupTimeoutCommit(this).Apply(blockCommit.Block, operation, content);
                            break;
                        default:
                            throw new NotImplementedException($"'{content.RequiredString("kind")}' is not expected in operations[3]");
                    }
                }
                Manager.Reset();
            }
            #endregion

            new InboxCommit(this).Apply(blockCommit.Block);

            await bigMapCommit.Apply();
            await ticketsCommit.Apply();
            await new TokensCommit(this).Apply(blockCommit.Block, bigMapCommit.Updates);

            var brCommit = new BakingRightsCommit(this);
            await brCommit.Apply(blockCommit.Block, cycleCommit.FutureCycle, cycleCommit.SelectedStakes);

            await new DelegatorCycleCommit(this).Apply(blockCommit.Block, cycleCommit.FutureCycle);

            await new BakerCycleCommit(this).Apply(
                blockCommit.Block,
                cycleCommit.FutureCycle,
                brCommit.FutureBakingRights,
                brCommit.FutureEndorsingRights,
                cycleCommit.Snapshots,
                cycleCommit.SelectedStakes,
                brCommit.CurrentRights);

            new FreezerCommit(this).Apply(blockCommit.Block, block);
            await new EndorsingRewardCommit(this).Apply(blockCommit.Block, block);
            await new VotingCommit(this).Apply(blockCommit.Block, block);
            await new StateCommit(this).Apply(blockCommit.Block, block);
        }

        public override async Task AfterCommit(JsonElement rawBlock)
        {
            var block = await Cache.Blocks.CurrentAsync();
            await new SnapshotBalanceCommit(this).Apply(rawBlock, block);
        }

        public override async Task BeforeRevert()
        {
            var block = await Cache.Blocks.CurrentAsync();
            await new SnapshotBalanceCommit(this).Revert(block);
        }

        public override async Task Revert()
        {
            var currBlock = await Cache.Blocks.CurrentAsync();
            Db.TryAttach(currBlock);

            await new VotingCommit(this).Revert(currBlock);
            await new StatisticsCommit(this).Revert(currBlock);

            await new EndorsingRewardCommit(this).Revert(currBlock);
            new FreezerCommit(this).Revert();

            await new BakerCycleCommit(this).Revert(currBlock);
            await new DelegatorCycleCommit(this).Revert(currBlock);
            await new BakingRightsCommit(this).Revert(currBlock);
            await new TokensCommit(this).Revert(currBlock);
            await new TicketsCommit(this).Revert(currBlock);
            await new BigMapCommit(this).Revert(currBlock);
            await new ContractEventCommit(this).Revert(currBlock);
            await new InboxCommit(this).Revert(currBlock);

            foreach (var operation in Context.EnumerateOps().OrderByDescending(x => x.Id).ToList())
            {
                switch (operation)
                {
                    case EndorsementOperation op:
                        await new EndorsementsCommit(this).Revert(currBlock, op);
                        break;
                    case PreendorsementOperation op:
                        await new PreendorsementsCommit(this).Revert(currBlock, op);
                        break;
                    case ProposalOperation op:
                        await new ProposalsCommit(this).Revert(currBlock, op);
                        break;
                    case BallotOperation op:
                        await new BallotsCommit(this).Revert(currBlock, op);
                        break;
                    case ActivationOperation op:
                        await new ActivationsCommit(this).Revert(currBlock, op);
                        break;
                    case DoubleBakingOperation op:
                        new DoubleBakingCommit(this).Revert(currBlock, op);
                        break;
                    case DoubleEndorsingOperation op:
                        new DoubleEndorsingCommit(this).Revert(currBlock, op);
                        break;
                    case DoublePreendorsingOperation op:
                        new DoublePreendorsingCommit(this).Revert(currBlock, op);
                        break;
                    case NonceRevelationOperation op:
                        await new NonceRevelationsCommit(this).Revert(currBlock, op);
                        break;
                    case VdfRevelationOperation op:
                        await new VdfRevelationCommit(this).Revert(currBlock, op);
                        break;
                    case DrainDelegateOperation op:
                        await new DrainDelegateCommit(this).Revert(currBlock, op);
                        break;
                    case RevealOperation op:
                        await new RevealsCommit(this).Revert(currBlock, op);
                        break;
                    case IncreasePaidStorageOperation op:
                        await new IncreasePaidStorageCommit(this).Revert(currBlock, op);
                        break;
                    case UpdateConsensusKeyOperation op:
                        await new UpdateConsensusKeyCommit(this).Revert(currBlock, op);
                        break;
                    case RegisterConstantOperation op:
                        await new RegisterConstantsCommit(this).Revert(currBlock, op);
                        break;
                    case SetDepositsLimitOperation op:
                        await new SetDepositsLimitCommit(this).Revert(currBlock, op);
                        break;
                    case DelegationOperation op:
                        if (op.InitiatorId == null)
                            await new DelegationsCommit(this).Revert(currBlock, op);
                        else
                            await new DelegationsCommit(this).RevertInternal(currBlock, op);
                        break;
                    case OriginationOperation op:
                        if (op.InitiatorId == null)
                            await new OriginationsCommit(this).Revert(currBlock, op);
                        else
                            await new OriginationsCommit(this).RevertInternal(currBlock, op);
                        break;
                    case TransactionOperation op:
                        if (op.InitiatorId == null)
                            await new TransactionsCommit(this).Revert(currBlock, op);
                        else
                            await new TransactionsCommit(this).RevertInternal(currBlock, op);
                        break;
                    case TxRollupOriginationOperation op:
                        await new TxRollupOriginationCommit(this).Revert(currBlock, op);
                        break;
                    case TxRollupSubmitBatchOperation op:
                        await new TxRollupSubmitBatchCommit(this).Revert(currBlock, op);
                        break;
                    case TxRollupCommitOperation op:
                        await new TxRollupCommitCommit(this).Revert(currBlock, op);
                        break;
                    case TxRollupFinalizeCommitmentOperation op:
                        await new TxRollupFinalizeCommitmentCommit(this).Revert(currBlock, op);
                        break;
                    case TxRollupRemoveCommitmentOperation op:
                        await new TxRollupRemoveCommitmentCommit(this).Revert(currBlock, op);
                        break;
                    case TxRollupReturnBondOperation op:
                        await new TxRollupReturnBondCommit(this).Revert(currBlock, op);
                        break;
                    case TxRollupRejectionOperation op:
                        await new TxRollupRejectionCommit(this).Revert(currBlock, op);
                        break;
                    case TxRollupDispatchTicketsOperation op:
                        await new TxRollupDispatchTicketsCommit(this).Revert(currBlock, op);
                        break;
                    case TransferTicketOperation op:
                        await new TransferTicketCommit(this).Revert(currBlock, op);
                        break;
                    case SmartRollupAddMessagesOperation op:
                        await new SmartRollupAddMessagesCommit(this).Revert(currBlock, op);
                        break;
                    case SmartRollupCementOperation op:
                        await new SmartRollupCementCommit(this).Revert(currBlock, op);
                        break;
                    case SmartRollupExecuteOperation op:
                        await new SmartRollupExecuteCommit(this).Revert(currBlock, op);
                        break;
                    case SmartRollupOriginateOperation op:
                        await new SmartRollupOriginateCommit(this).Revert(currBlock, op);
                        break;
                    case SmartRollupPublishOperation op:
                        await new SmartRollupPublishCommit(this).Revert(currBlock, op);
                        break;
                    case SmartRollupRecoverBondOperation op:
                        await new SmartRollupRecoverBondCommit(this).Revert(currBlock, op);
                        break;
                    case SmartRollupRefuteOperation op:
                        await new SmartRollupRefuteCommit(this).Revert(currBlock, op);
                        break;
                    default:
                        throw new NotImplementedException($"'{operation.GetType()}' is not implemented");
                }
            }

            await new SubsidyCommit(this).Revert(currBlock);

            await new DeactivationCommit(this).Revert(currBlock);
            await new SoftwareCommit(this).Revert(currBlock);
            await new CycleCommit(this).Revert(currBlock);
            await new BlockCommit(this).Revert(currBlock);

            await new StateCommit(this).Revert(currBlock);
        }
    }
}
