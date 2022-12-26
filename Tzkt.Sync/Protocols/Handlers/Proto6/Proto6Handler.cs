using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using App.Metrics;

using Tzkt.Data;
using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;
using Tzkt.Sync.Services;
using Tzkt.Sync.Protocols.Proto6;

namespace Tzkt.Sync.Protocols
{
    class Proto6Handler : ProtocolHandler
    {
        public override IDiagnostics Diagnostics { get; }
        public override IValidator Validator { get; }
        public override IRpc Rpc { get; }

        public Proto6Handler(TezosNode node, TzktContext db, CacheService cache, QuotesService quotes, IServiceProvider services, IConfiguration config, ILogger<Proto6Handler> logger, IMetrics metrics)
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
            var blockCommit = new BlockCommit(this);
            await blockCommit.Apply(block);

            await new SoftwareCommit(this).Apply(blockCommit.Block, block);

            var freezerCommit = new FreezerCommit(this);
            await freezerCommit.Apply(blockCommit.Block, block);

            await new RevelationPenaltyCommit(this).Apply(blockCommit.Block, block);
            await new DeactivationCommit(this).Apply(blockCommit.Block, block);

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
                        default:
                            throw new NotImplementedException($"'{content.RequiredString("kind")}' is not allowed in operations[0]");
                    }
                }
            }
            #endregion

            #region operations 1
            foreach (var operation in operations[1].EnumerateArray())
            {
                foreach (var content in operation.RequiredArray("contents", 1).EnumerateArray())
                {
                    switch (content.RequiredString("kind"))
                    {
                        case "proposals":
                            await new ProposalsCommit(this).Apply(blockCommit.Block, operation, content);
                            break;
                        case "ballot":
                            await new BallotsCommit(this).Apply(blockCommit.Block, operation, content);
                            break;
                        default:
                            throw new NotImplementedException($"'{content.RequiredString("kind")}' is not allowed in operations[1]");
                    }
                }
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
                            await new DoubleBakingCommit(this).Apply(blockCommit.Block, operation, content);
                            break;
                        case "double_endorsement_evidence":
                            await new DoubleEndorsingCommit(this).Apply(blockCommit.Block, operation, content);
                            break;
                        case "seed_nonce_revelation":
                            await new NonceRevelationsCommit(this).Apply(blockCommit.Block, operation, content);
                            break;
                        default:
                            throw new NotImplementedException($"'{content.RequiredString("kind")}' is not allowed in operations[2]");
                    }
                }
            }
            #endregion

            var bigMapCommit = new BigMapCommit(this);

            #region operations 3
            foreach (var operation in operations[3].EnumerateArray())
            {
                Manager.Init(operation);
                foreach (var content in operation.RequiredArray("contents").EnumerateArray())
                {
                    switch (content.RequiredString("kind"))
                    {
                        case "reveal":
                            await new RevealsCommit(this).Apply(blockCommit.Block, operation, content);
                            break;
                        case "delegation":
                            await new DelegationsCommit(this).Apply(blockCommit.Block, operation, content);
                            break;
                        case "origination":
                            var orig = new OriginationsCommit(this);
                            await orig.Apply(blockCommit.Block, operation, content);
                            if (orig.BigMapDiffs != null)
                                bigMapCommit.Append(orig.Origination, orig.Origination.Contract, orig.BigMapDiffs);
                            break;
                        case "transaction":
                            var parent = new TransactionsCommit(this);
                            await parent.Apply(blockCommit.Block, operation, content);
                            if (parent.BigMapDiffs != null)
                                bigMapCommit.Append(parent.Transaction, parent.Transaction.Target as Contract, parent.BigMapDiffs);

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
                                                bigMapCommit.Append(internalOrig.Origination, internalOrig.Origination.Contract, internalOrig.BigMapDiffs);
                                            break;
                                        case "transaction":
                                            var internalTx = new TransactionsCommit(this);
                                            await internalTx.ApplyInternal(blockCommit.Block, parent.Transaction, internalContent);
                                            if (internalTx.BigMapDiffs != null)
                                                bigMapCommit.Append(internalTx.Transaction, internalTx.Transaction.Target as Contract, internalTx.BigMapDiffs);
                                            break;
                                        default:
                                            throw new NotImplementedException($"internal '{content.RequiredString("kind")}' is not implemented");
                                    }
                                }
                            }
                            break;
                        default:
                            throw new NotImplementedException($"'{content.RequiredString("kind")}' is not expected in operations[3]");
                    }
                }
                Manager.Reset();
            }
            #endregion

            await bigMapCommit.Apply();
            await new TokensCommit(this).Apply(blockCommit.Block, bigMapCommit.Updates);

            var brCommit = new BakingRightsCommit(this);
            await brCommit.Apply(blockCommit.Block);

            var cycleCommit = new CycleCommit(this);
            await cycleCommit.Apply(blockCommit.Block);

            await new DelegatorCycleCommit(this).Apply(blockCommit.Block, cycleCommit.FutureCycle);

            await new BakerCycleCommit(this).Apply(
                blockCommit.Block,
                cycleCommit.FutureCycle,
                brCommit.FutureBakingRights,
                brCommit.FutureEndorsingRights,
                cycleCommit.Snapshots,
                brCommit.CurrentRights);

            await new StatisticsCommit(this).Apply(blockCommit.Block, freezerCommit.FreezerUpdates);
            await new VotingCommit(this).Apply(blockCommit.Block, block);
            await new StateCommit(this).Apply(blockCommit.Block, block);
        }

        public override async Task AfterCommit(JsonElement rawBlock)
        {
            var block = await Cache.Blocks.CurrentAsync();
            await new SnapshotBalanceCommit(this).Apply(block, rawBlock);
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

            #region load operations
            var operations = new List<BaseOperation>(40);

            if (currBlock.Operations.HasFlag(Operations.Activations))
                operations.AddRange(await Db.ActivationOps.Where(x => x.Level == currBlock.Level).ToListAsync());

            if (currBlock.Operations.HasFlag(Operations.Delegations))
                operations.AddRange(await Db.DelegationOps.Where(x => x.Level == currBlock.Level).ToListAsync());

            if (currBlock.Operations.HasFlag(Operations.Endorsements))
                operations.AddRange(await Db.EndorsementOps.Where(x => x.Level == currBlock.Level).ToListAsync());

            if (currBlock.Operations.HasFlag(Operations.Originations))
                operations.AddRange(await Db.OriginationOps.Where(x => x.Level == currBlock.Level).ToListAsync());

            if (currBlock.Operations.HasFlag(Operations.Reveals))
                operations.AddRange(await Db.RevealOps.Where(x => x.Level == currBlock.Level).ToListAsync());

            if (currBlock.Operations.HasFlag(Operations.Revelations))
                operations.AddRange(await Db.NonceRevelationOps.Where(x => x.Level == currBlock.Level).ToListAsync());

            if (currBlock.Operations.HasFlag(Operations.Transactions))
                operations.AddRange(await Db.TransactionOps.Where(x => x.Level == currBlock.Level).ToListAsync());

            if (currBlock.Operations.HasFlag(Operations.DoubleBakings))
                operations.AddRange(await Db.DoubleBakingOps.Where(x => x.Level == currBlock.Level).ToListAsync());

            if (currBlock.Operations.HasFlag(Operations.DoubleEndorsings))
                operations.AddRange(await Db.DoubleEndorsingOps.Where(x => x.Level == currBlock.Level).ToListAsync());

            if (currBlock.Operations.HasFlag(Operations.Ballots))
                operations.AddRange(await Db.BallotOps.Where(x => x.Level == currBlock.Level).ToListAsync());

            if (currBlock.Operations.HasFlag(Operations.Proposals))
                operations.AddRange(await Db.ProposalOps.Where(x => x.Level == currBlock.Level).ToListAsync());

            if (currBlock.Operations.HasFlag(Operations.RevelationPenalty))
                await Db.Entry(currBlock).Collection(x => x.RevelationPenalties).LoadAsync();

            if (currBlock.Events.HasFlag(BlockEvents.NewAccounts))
            {
                await Db.Entry(currBlock).Collection(x => x.CreatedAccounts).LoadAsync();
                foreach (var account in currBlock.CreatedAccounts)
                    Cache.Accounts.Add(account);
            }
            #endregion

            await new VotingCommit(this).Revert(currBlock);
            await new StatisticsCommit(this).Revert(currBlock);

            await new BakerCycleCommit(this).Revert(currBlock);
            await new DelegatorCycleCommit(this).Revert(currBlock);
            await new CycleCommit(this).Revert(currBlock);
            await new BakingRightsCommit(this).Revert(currBlock);
            await new TokensCommit(this).Revert(currBlock);
            await new BigMapCommit(this).Revert(currBlock);

            foreach (var operation in operations.OrderByDescending(x => x.Id))
            {
                switch (operation)
                {
                    case EndorsementOperation endorsement:
                        await new EndorsementsCommit(this).Revert(currBlock, endorsement);
                        break;
                    case ProposalOperation proposal:
                        await new ProposalsCommit(this).Revert(currBlock, proposal);
                        break;
                    case BallotOperation ballot:
                        await new BallotsCommit(this).Revert(currBlock, ballot);
                        break;
                    case ActivationOperation activation:
                        await new ActivationsCommit(this).Revert(currBlock, activation);
                        break;
                    case DoubleBakingOperation doubleBaking:
                        await new DoubleBakingCommit(this).Revert(currBlock, doubleBaking);
                        break;
                    case DoubleEndorsingOperation doubleEndorsing:
                        await new DoubleEndorsingCommit(this).Revert(currBlock, doubleEndorsing);
                        break;
                    case NonceRevelationOperation revelation:
                        await new NonceRevelationsCommit(this).Revert(currBlock, revelation);
                        break;
                    case RevealOperation reveal:
                        await new RevealsCommit(this).Revert(currBlock, reveal);
                        break;
                    case DelegationOperation delegation:
                        if (delegation.InitiatorId == null)
                            await new DelegationsCommit(this).Revert(currBlock, delegation);
                        else
                            await new DelegationsCommit(this).RevertInternal(currBlock, delegation);
                        break;
                    case OriginationOperation origination:
                        if (origination.InitiatorId == null)
                            await new OriginationsCommit(this).Revert(currBlock, origination);
                        else
                            await new OriginationsCommit(this).RevertInternal(currBlock, origination);
                        break;
                    case TransactionOperation transaction:
                        if (transaction.InitiatorId == null)
                            await new TransactionsCommit(this).Revert(currBlock, transaction);
                        else
                            await new TransactionsCommit(this).RevertInternal(currBlock, transaction);
                        break;
                    default:
                        throw new NotImplementedException($"'{operation.GetType()}' is not implemented");
                }
            }

            await new DeactivationCommit(this).Revert(currBlock);
            await new RevelationPenaltyCommit(this).Revert(currBlock);
            await new FreezerCommit(this).Revert(currBlock);
            await new SoftwareCommit(this).Revert(currBlock);
            await new BlockCommit(this).Revert(currBlock);

            await new StateCommit(this).Revert(currBlock);
        }
    }
}
