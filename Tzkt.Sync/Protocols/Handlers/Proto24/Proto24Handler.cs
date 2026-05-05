using System.Text.Json;
using App.Metrics;
using Tzkt.Data;
using Tzkt.Data.Models;
using Tzkt.Sync.Services;
using Tzkt.Sync.Protocols.Proto24;

namespace Tzkt.Sync.Protocols
{
    class Proto24Handler(
        TezosNode node,
        TzktContext db,
        CacheService cache,
        QuotesService quotes,
        IServiceProvider services,
        IConfiguration config,
        ILogger<Proto24Handler> logger,
        IMetrics metrics) : ProtocolHandler(node, db, cache, quotes, services, config, logger, metrics)
    {
        public override IActivator Activator => new ProtoActivator(this);
        public override IMigrator Migrator => new ProtoMigrator(this);
        public override IValidator Validator => new Validator(this);
        public override IRpc Rpc { get; } = new Rpc(node);
        public override string VersionName => "t024_024";
        public override int VersionNumber => 24;

        public override async Task Commit(JsonElement block)
        {
            await new StatisticsCommit(this).Apply(block);

            var blockCommit = new BlockCommit(this);
            await blockCommit.Apply(block);

            var operations = block.RequiredArray("operations", 4);

            #region operations 0
            if (operations[0].EnumerateArray().Any())
                throw new NotImplementedException($"Consensus operations are not allowed");
            #endregion

            #region operations 1
            if (operations[1].EnumerateArray().Any())
                throw new NotImplementedException($"Governance operations are not allowed");
            #endregion

            #region operations 2
            if (operations[2].EnumerateArray().Any())
                throw new NotImplementedException($"Anonymous operations are not allowed");
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
                        case "increase_paid_storage":
                            await new IncreasePaidStorageCommit(this).Apply(blockCommit.Block, operation, content);
                            break;
                        case "reveal":
                            await new RevealsCommit(this).Apply(blockCommit.Block, operation, content);
                            break;
                        case "register_global_constant":
                            await new RegisterConstantsCommit(this).Apply(blockCommit.Block, operation, content);
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
                                            if (internalContent.RequiredString("source") != NullAddress.Address)
                                                await new ContractEventCommit(this).Apply(blockCommit.Block, internalContent);
                                            break;
                                        default:
                                            throw new NotImplementedException($"internal '{internalContent.RequiredString("kind")}' is not implemented");
                                    }
                                }
                            }
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
                                            if (internalContent.RequiredString("source") != NullAddress.Address)
                                                await new ContractEventCommit(this).Apply(blockCommit.Block, internalContent);
                                            break;
                                        default:
                                            throw new NotImplementedException($"internal '{internalContent.RequiredString("kind")}' inside 'transfer_ticket' is not expected");
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
            await ticketsCommit.Apply();
            await new TokensCommit(this).Apply(blockCommit.Block, bigMapCommit.Updates);

            await new StateCommit(this).Apply(blockCommit.Block, block);
        }

        public override async Task Revert()
        {
            var currBlock = await Cache.Blocks.CurrentAsync();
            Db.TryAttach(currBlock);

            await new StatisticsCommit(this).Revert(currBlock);

            await new TokensCommit(this).Revert(currBlock);
            await new TicketsCommit(this).Revert(currBlock);
            await new BigMapCommit(this).Revert(currBlock);
            await new ContractEventCommit(this).Revert(currBlock);

            foreach (var operation in Context.EnumerateOps().OrderByDescending(x => x.Id).ToList())
            {
                switch (operation)
                {
                    case RevealOperation op:
                        await new RevealsCommit(this).Revert(currBlock, op);
                        break;
                    case IncreasePaidStorageOperation op:
                        await new IncreasePaidStorageCommit(this).Revert(currBlock, op);
                        break;
                    case RegisterConstantOperation op:
                        await new RegisterConstantsCommit(this).Revert(currBlock, op);
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
                    case TransferTicketOperation op:
                        await new TransferTicketCommit(this).Revert(currBlock, op);
                        break;
                    default:
                        throw new NotImplementedException($"'{operation.GetType()}' is not implemented");
                }
            }

            new BlockCommit(this).Revert(currBlock);

            await new StateCommit(this).Revert();
        }
    }
}
