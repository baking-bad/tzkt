using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Tzkt.Data;
using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;
using Tzkt.Sync.Services;
using Tzkt.Sync.Protocols.Proto1;

namespace Tzkt.Sync.Protocols
{
    class Proto1Handler : ProtocolHandler
    {
        public override string Protocol => "Proto 1";
        public override IDiagnostics Diagnostics { get; }
        public override ISerializer Serializer { get; }
        public override IValidator Validator { get; }

        public Proto1Handler(TezosNode node, TzktContext db, CacheService cache, IConfiguration config, ILogger<Proto1Handler> logger)
            : base(node, db, cache, config, logger)
        {
            Diagnostics = new Diagnostics(db, node);
            Serializer = new Serializer();
            Validator = new Validator(this);
        }

        public override Task Migration() => Proto2.DelegatesMigration.Apply(this);

        public override Task CancelMigration() => Proto2.DelegatesMigration.Revert(this);

        public override Task LoadEntities(IBlock block)
        {
            var rawBlock = block as RawBlock;
            var accounts = new HashSet<string>(64);

            foreach (var op in rawBlock.Operations[2])
            {
                if (op.Contents[0] is RawActivationContent activation)
                    accounts.Add(activation.Address);
            }

            foreach (var op in rawBlock.Operations[3])
            {
                foreach (var content in op.Contents)
                {
                    switch (content)
                    {
                        case RawRevealContent reveal:
                            accounts.Add(reveal.Source);
                            break;
                        case RawDelegationContent delegation:
                            accounts.Add(delegation.Source);
                            break;
                        case RawTransactionContent transaction:
                            accounts.Add(transaction.Source);
                            if (transaction.Destination != null)
                                accounts.Add(transaction.Destination);

                            if (transaction.Metadata.InternalResults != null)
                                foreach (var internalContent in transaction.Metadata.InternalResults)
                                {
                                    if (internalContent is RawInternalTransactionResult internalTransaction)
                                    {
                                        accounts.Add(internalTransaction.Source);
                                        if (internalTransaction.Destination != null)
                                            accounts.Add(internalTransaction.Destination);
                                    }
                                }
                            break;
                        case RawOriginationContent origination:
                            accounts.Add(origination.Source);
                            if (origination.Manager != null)
                                accounts.Add(origination.Manager);
                            break;
                    }
                }
            }

            return Cache.Accounts.LoadAsync(accounts);
        }

        public override async Task InitProtocol(IBlock block)
        {
            var state = Cache.AppState.Get();
            var currProtocol = await Cache.Protocols.GetAsync(state.Protocol);

            Protocol protocol = null;
            if (state.Protocol != state.NextProtocol)
            {
                protocol = new Protocol
                {
                    Hash = block.Protocol,
                    Code = await Db.Protocols.CountAsync() - 1,
                    FirstLevel = block.Level,
                    LastLevel = -1
                };
                Db.Protocols.Add(protocol);
                Cache.Protocols.Add(protocol);
            }
            else if (block.Level % currProtocol.BlocksPerCycle == 1)
            {
                protocol = await Cache.Protocols.GetAsync(state.Protocol);
                Db.TryAttach(protocol);
            }

            if (protocol != null)
            {
                var stream = await Node.GetConstantsAsync(block.Level);
                var rawConst = await (Serializer as Serializer).DeserializeConstants(stream);

                protocol.BlockDeposit = rawConst.BlockDeposit;
                protocol.BlockReward0 = rawConst.BlockReward;
                protocol.BlocksPerCommitment = rawConst.BlocksPerCommitment;
                protocol.BlocksPerCycle = rawConst.BlocksPerCycle;
                protocol.BlocksPerSnapshot = rawConst.BlocksPerSnapshot;
                protocol.BlocksPerVoting = rawConst.BlocksPerVoting;
                protocol.ByteCost = rawConst.ByteCost;
                protocol.EndorsementDeposit = rawConst.EndorsementDeposit;
                protocol.EndorsementReward0 = rawConst.EndorsementReward;
                protocol.EndorsersPerBlock = rawConst.EndorsersPerBlock;
                protocol.HardBlockGasLimit = rawConst.HardBlockGasLimit;
                protocol.HardOperationGasLimit = rawConst.HardOperationGasLimit;
                protocol.HardOperationStorageLimit = rawConst.HardOperationStorageLimit;
                protocol.OriginationSize = rawConst.OriginationBurn / rawConst.ByteCost;
                protocol.PreservedCycles = rawConst.PreservedCycles;
                protocol.RevelationReward = rawConst.RevelationReward;
                protocol.TimeBetweenBlocks = rawConst.TimeBetweenBlocks[0];
                protocol.TokensPerRoll = rawConst.TokensPerRoll;
            }
        }

        public override async Task InitProtocol()
        {
            var state = Cache.AppState.Get();
            var currProtocol = await Cache.Protocols.GetAsync(state.Protocol);

            if (state.Protocol == state.NextProtocol &&
                state.Level % currProtocol.BlocksPerCycle != 0)
                return;

            var stream = await Node.GetConstantsAsync(state.Level - 1);
            var rawConst = await(Serializer as Serializer).DeserializeConstants(stream);

            Db.TryAttach(currProtocol);

            currProtocol.BlockDeposit = rawConst.BlockDeposit;
            currProtocol.BlockReward0 = rawConst.BlockReward;
            currProtocol.BlocksPerCommitment = rawConst.BlocksPerCommitment;
            currProtocol.BlocksPerCycle = rawConst.BlocksPerCycle;
            currProtocol.BlocksPerSnapshot = rawConst.BlocksPerSnapshot;
            currProtocol.BlocksPerVoting = rawConst.BlocksPerVoting;
            currProtocol.ByteCost = rawConst.ByteCost;
            currProtocol.EndorsementDeposit = rawConst.EndorsementDeposit;
            currProtocol.EndorsementReward0 = rawConst.EndorsementReward;
            currProtocol.EndorsersPerBlock = rawConst.EndorsersPerBlock;
            currProtocol.HardBlockGasLimit = rawConst.HardBlockGasLimit;
            currProtocol.HardOperationGasLimit = rawConst.HardOperationGasLimit;
            currProtocol.HardOperationStorageLimit = rawConst.HardOperationStorageLimit;
            currProtocol.OriginationSize = rawConst.OriginationBurn / rawConst.ByteCost;
            currProtocol.PreservedCycles = rawConst.PreservedCycles;
            currProtocol.RevelationReward = rawConst.RevelationReward;
            currProtocol.TimeBetweenBlocks = rawConst.TimeBetweenBlocks[0];
            currProtocol.TokensPerRoll = rawConst.TokensPerRoll;
        }

        public override async Task Commit(IBlock block)
        {
            var rawBlock = block as RawBlock;

            var blockCommit = await BlockCommit.Apply(this, rawBlock);
            await FreezerCommit.Apply(this, blockCommit.Block, rawBlock);

            #region operations 0
            foreach (var operation in rawBlock.Operations[0])
            {
                foreach (var content in operation.Contents)
                {
                    switch (content)
                    {
                        case RawEndorsementContent endorsement:
                            await EndorsementsCommit.Apply(this, blockCommit.Block, operation, endorsement);
                            break;
                        default:
                            throw new NotImplementedException($"'{content.GetType()}' is not allowed in operations[0]");
                    }
                }
            }
            #endregion

            #region operations 1
            foreach (var operation in rawBlock.Operations[1])
            {
                foreach (var content in operation.Contents)
                {
                    throw new NotImplementedException($"'{content.GetType()}' is not implemented");
                }
            }
            #endregion

            #region operations 2
            foreach (var operation in rawBlock.Operations[2])
            {
                foreach (var content in operation.Contents)
                {
                    switch (content)
                    {
                        case RawActivationContent activation:
                            await ActivationsCommit.Apply(this, blockCommit.Block, operation, activation);
                            break;
                        case RawNonceRevelationContent revelation:
                            await NonceRevelationsCommit.Apply(this, blockCommit.Block, operation, revelation);
                            break;
                        default:
                            throw new NotImplementedException($"'{content.GetType()}' is not allowed in operations[2]");
                    }
                }
            }
            #endregion

            #region operations 3
            foreach (var operation in rawBlock.Operations[3])
            {
                Cache.AppState.IncreaseManagerCounter(operation.Contents.Count);

                foreach (var content in operation.Contents)
                {
                    switch (content)
                    {
                        case RawRevealContent reveal:
                            await RevealsCommit.Apply(this, blockCommit.Block, operation, reveal);
                            break;
                        case RawDelegationContent delegation:
                            await DelegationsCommit.Apply(this, blockCommit.Block, operation, delegation);
                            break;
                        case RawOriginationContent origination:
                            await OriginationsCommit.Apply(this, blockCommit.Block, operation, origination);
                            break;
                        case RawTransactionContent transaction:
                            var parent = await TransactionsCommit.Apply(this, blockCommit.Block, operation, transaction);
                            if (transaction.Metadata.InternalResults != null)
                            {
                                foreach (var internalContent in transaction.Metadata.InternalResults)
                                {
                                    switch (internalContent)
                                    {
                                        case RawInternalTransactionResult internalTransaction:
                                            await TransactionsCommit.Apply(this, blockCommit.Block, parent.Transaction, internalTransaction);
                                            break;
                                        default:
                                            throw new NotImplementedException($"internal '{content.GetType()}' is not implemented");
                                    }
                                }
                            }
                            break;
                        default:
                            throw new NotImplementedException($"'{content.GetType()}' is not expected in operations[3]");
                    }
                }
            }
            #endregion

            var brCommit = await BakingRightsCommit.Apply(this, blockCommit.Block);
            var cycleCommit = await CycleCommit.Apply(this, blockCommit.Block);
            await DelegatorCycleCommit.Apply(this, blockCommit.Block, cycleCommit.FutureCycle);

            await BakerCycleCommit.Apply(this,
                blockCommit.Block,
                cycleCommit.FutureCycle,
                brCommit.FutureBakingRights,
                brCommit.FutureEndorsingRights,
                cycleCommit.Snapshots,
                brCommit.CurrentRights);

            await StateCommit.Apply(this, blockCommit.Block, rawBlock);
        }

        public override async Task AfterCommit(IBlock rawBlock)
        {
            var block = await Cache.Blocks.CurrentAsync();
            await SnapshotBalanceCommit.Apply(this, block);
        }

        public override async Task BeforeRevert()
        {
            var block = await Cache.Blocks.CurrentAsync();
            await SnapshotBalanceCommit.Revert(this, block);
        }

        public override async Task Revert()
        {
            var currBlock = await Cache.Blocks.CurrentAsync();

            #region load operations
            var query = Db.Blocks.AsQueryable();

            if (currBlock.Operations.HasFlag(Operations.Activations))
                query = query.Include(x => x.Activations);

            if (currBlock.Operations.HasFlag(Operations.Delegations))
                query = query.Include(x => x.Delegations);

            if (currBlock.Operations.HasFlag(Operations.Endorsements))
                query = query.Include(x => x.Endorsements);

            if (currBlock.Operations.HasFlag(Operations.Originations))
                query = query.Include(x => x.Originations);

            if (currBlock.Operations.HasFlag(Operations.Reveals))
                query = query.Include(x => x.Reveals);

            if (currBlock.Operations.HasFlag(Operations.Revelations))
                query = query.Include(x => x.Revelations);

            if (currBlock.Operations.HasFlag(Operations.Transactions))
                query = query.Include(x => x.Transactions);

            if (currBlock.Events.HasFlag(BlockEvents.NewAccounts))
                query = query.Include(x => x.CreatedAccounts);

            currBlock = await query.FirstOrDefaultAsync(x => x.Level == currBlock.Level);
            Cache.Blocks.Add(currBlock);

            var operations = new List<BaseOperation>(40);
            if (currBlock.Activations != null)
                operations.AddRange(currBlock.Activations);

            if (currBlock.Delegations != null)
                operations.AddRange(currBlock.Delegations);

            if (currBlock.Endorsements != null)
                operations.AddRange(currBlock.Endorsements);

            if (currBlock.Originations != null)
                operations.AddRange(currBlock.Originations);

            if (currBlock.Reveals != null)
                operations.AddRange(currBlock.Reveals);

            if (currBlock.Revelations != null)
                operations.AddRange(currBlock.Revelations);

            if (currBlock.Transactions != null)
                operations.AddRange(currBlock.Transactions);

            if (currBlock.CreatedAccounts != null)
                foreach (var account in currBlock.CreatedAccounts)
                    Cache.Accounts.Add(account);
            #endregion

            await BakerCycleCommit.Revert(this, currBlock);
            await DelegatorCycleCommit.Revert(this, currBlock);
            await CycleCommit.Revert(this, currBlock);
            await BakingRightsCommit.Revert(this, currBlock);

            foreach (var operation in operations.OrderByDescending(x => x.Id))
            {
                switch (operation)
                {
                    case EndorsementOperation endorsement:
                        await EndorsementsCommit.Revert(this, currBlock, endorsement);
                        break;
                    case ActivationOperation activation:
                        await ActivationsCommit.Revert(this, currBlock, activation);
                        break;
                    case NonceRevelationOperation revelation:
                        await NonceRevelationsCommit.Revert(this, currBlock, revelation);
                        break;
                    case RevealOperation reveal:
                        await RevealsCommit.Revert(this, currBlock, reveal);
                        break;
                    case DelegationOperation delegation:
                        await DelegationsCommit.Revert(this, currBlock, delegation);
                        break;
                    case OriginationOperation origination:
                        await OriginationsCommit.Revert(this, currBlock, origination);
                        break;
                    case TransactionOperation transaction:
                        await TransactionsCommit.Revert(this, currBlock, transaction);
                        break;
                    default:
                        throw new NotImplementedException($"'{operation.GetType()}' is not implemented");
                }
            }

            await FreezerCommit.Revert(this, currBlock);
            await BlockCommit.Revert(this, currBlock);

            await StateCommit.Revert(this, currBlock);
        }
    }
}
