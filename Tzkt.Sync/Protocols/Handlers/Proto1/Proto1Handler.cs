using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using Microsoft.EntityFrameworkCore;

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
        public override ISerializer Serializer { get; }
        public override IValidator Validator { get; }

        public Proto1Handler(TezosNode node, TzktContext db, CacheService cache, DiagnosticService diagnostics, ILogger<Proto1Handler> logger)
            : base(node, db, cache, diagnostics, logger)
        {
            Serializer = new Serializer();
            Validator = new Validator(this);
        }

        public override Task Migration() => Proto2.DelegatesMigration.Apply(this);

        public override Task CancelMigration() => Proto2.DelegatesMigration.Revert(this);

        public override async Task InitProtocol(IBlock block)
        {
            var state = await Cache.GetAppStateAsync();
            var currProtocol = await Cache.GetProtocolAsync(state.Protocol);

            Protocol protocol = null;
            if (state.Protocol != state.NextProtocol)
            {
                protocol = new Protocol
                {
                    Hash = block.Protocol,
                    Code = await Db.Protocols.CountAsync() - 1,
                };
                Db.Protocols.Add(protocol);
                Cache.AddProtocol(protocol);
            }
            else if (block.Level % currProtocol.BlocksPerCycle == 1)
            {
                protocol = await Cache.GetProtocolAsync(state.Protocol);
                Db.TryAttach(protocol);
            }

            if (protocol != null)
            {
                var stream = await Node.GetConstantsAsync(block.Level);
                var rawConst = await (Serializer as Serializer).DeserializeConstants(stream);

                protocol.BlockDeposit = rawConst.BlockDeposit;
                protocol.BlockReward = rawConst.BlockReward;
                protocol.BlocksPerCommitment = rawConst.BlocksPerCommitment;
                protocol.BlocksPerCycle = rawConst.BlocksPerCycle;
                protocol.BlocksPerSnapshot = rawConst.BlocksPerSnapshot;
                protocol.BlocksPerVoting = rawConst.BlocksPerVoting;
                protocol.ByteCost = rawConst.ByteCost;
                protocol.EndorsementDeposit = rawConst.EndorsementDeposit;
                protocol.EndorsementReward = rawConst.EndorsementReward;
                protocol.EndorsersPerBlock = rawConst.EndorsersPerBlock;
                protocol.HardBlockGasLimit = rawConst.HardBlockGasLimit;
                protocol.HardOperationGasLimit = rawConst.HardOperationGasLimit;
                protocol.HardOperationStorageLimit = rawConst.HardOperationStorageLimit;
                protocol.OriginationSize = rawConst.OriginationBurn / rawConst.ByteCost;
                protocol.PreserverCycles = rawConst.PreserverCycles;
                protocol.RevelationReward = rawConst.RevelationReward;
                protocol.TimeBetweenBlocks = rawConst.TimeBetweenBlocks[0];
                protocol.TokensPerRoll = rawConst.TokensPerRoll;
            }
        }

        public override async Task InitProtocol()
        {
            var state = await Cache.GetAppStateAsync();
            var currProtocol = await Cache.GetProtocolAsync(state.Protocol);

            if (state.Protocol == state.NextProtocol &&
                state.Level % currProtocol.BlocksPerCycle != 0)
                return;

            var stream = await Node.GetConstantsAsync(state.Level - 1);
            var rawConst = await(Serializer as Serializer).DeserializeConstants(stream);

            Db.TryAttach(currProtocol);

            currProtocol.BlockDeposit = rawConst.BlockDeposit;
            currProtocol.BlockReward = rawConst.BlockReward;
            currProtocol.BlocksPerCommitment = rawConst.BlocksPerCommitment;
            currProtocol.BlocksPerCycle = rawConst.BlocksPerCycle;
            currProtocol.BlocksPerSnapshot = rawConst.BlocksPerSnapshot;
            currProtocol.BlocksPerVoting = rawConst.BlocksPerVoting;
            currProtocol.ByteCost = rawConst.ByteCost;
            currProtocol.EndorsementDeposit = rawConst.EndorsementDeposit;
            currProtocol.EndorsementReward = rawConst.EndorsementReward;
            currProtocol.EndorsersPerBlock = rawConst.EndorsersPerBlock;
            currProtocol.HardBlockGasLimit = rawConst.HardBlockGasLimit;
            currProtocol.HardOperationGasLimit = rawConst.HardOperationGasLimit;
            currProtocol.HardOperationStorageLimit = rawConst.HardOperationStorageLimit;
            currProtocol.OriginationSize = rawConst.OriginationBurn / rawConst.ByteCost;
            currProtocol.PreserverCycles = rawConst.PreserverCycles;
            currProtocol.RevelationReward = rawConst.RevelationReward;
            currProtocol.TimeBetweenBlocks = rawConst.TimeBetweenBlocks[0];
            currProtocol.TokensPerRoll = rawConst.TokensPerRoll;
        }

        public override async Task Commit(IBlock block)
        {
            var rawBlock = block as RawBlock;

            await ProtoCommit.Apply(this, rawBlock);
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
                await Cache.IncreaseManagerCounter(operation.Contents.Count);

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

            await StateCommit.Apply(this, blockCommit.Block, rawBlock);
        }

        public override async Task Revert()
        {
            var currBlock = await Cache.GetCurrentBlockAsync();

            #region load operations
            var query = Db.Blocks.AsQueryable();

            if (currBlock.Operations.HasFlag(Operations.Activations))
                query = query.Include(x => x.Activations);

            if (currBlock.Operations.HasFlag(Operations.Delegations))
                query = query.Include(x => x.Delegations).ThenInclude(x => x.DelegateChange);

            if (currBlock.Operations.HasFlag(Operations.Endorsements))
                query = query.Include(x => x.Endorsements);

            if (currBlock.Operations.HasFlag(Operations.Originations))
                query = query.Include(x => x.Originations).ThenInclude(x => x.WeirdDelegation);

            if (currBlock.Operations.HasFlag(Operations.Reveals))
                query = query.Include(x => x.Reveals);

            if (currBlock.Operations.HasFlag(Operations.Revelations))
                query = query.Include(x => x.Revelations);

            if (currBlock.Operations.HasFlag(Operations.Transactions))
                query = query.Include(x => x.Transactions);

            currBlock = await query.FirstOrDefaultAsync(x => x.Level == currBlock.Level);
            Cache.AddBlock(currBlock);

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
            #endregion

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
            await ProtoCommit.Revert(this, currBlock);

            await StateCommit.Revert(this, currBlock);
        }
    }
}
