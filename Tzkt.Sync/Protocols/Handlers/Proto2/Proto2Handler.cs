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
using Tzkt.Sync.Protocols.Proto2;

namespace Tzkt.Sync.Protocols
{
    class Proto2Handler : ProtocolHandler
    {
        public override string Protocol => "Proto 2";
        public override ISerializer Serializer { get; }
        public override IValidator Validator { get; }

        public Proto2Handler(TezosNode node, TzktContext db, CacheService cache, DiagnosticService diagnostics, ILogger<Proto1Handler> logger)
            : base(node, db, cache, diagnostics, logger)
        {
            Serializer = new Serializer();
            Validator = new Validator(this);
        }
        
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
                #region update constants
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
                #endregion
            }
        }

        public override async Task InitProtocol()
        {
            var state = await Cache.GetAppStateAsync();
            var currProtocol = await Cache.GetProtocolAsync(state.Protocol);

            if (state.Protocol == state.NextProtocol &&
                state.Level % currProtocol.BlocksPerCycle != 0)
                return;

            Db.TryAttach(currProtocol);

            #region update constants
            var stream = await Node.GetConstantsAsync(state.Level - 1);
            var rawConst = await (Serializer as Serializer).DeserializeConstants(stream);

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
            #endregion
        }

        public override async Task Commit(IBlock block)
        {
            var rawBlock = block as RawBlock;

            await ProtoCommit.Apply(this, rawBlock);
            var blockCommit = await BlockCommit.Apply(this, rawBlock);
            await FreezerCommit.Apply(this, rawBlock);
            await VotingCommit.Apply(this, rawBlock);
            await DeactivationCommit.Apply(this, rawBlock);

            foreach (var operation in rawBlock.Operations.SelectMany(x => x))
            {
                foreach (var content in operation.Contents)
                {
                    switch (content)
                    {
                        case RawEndorsementContent endorsement:
                            await EndorsementsCommit.Apply(this, blockCommit.Block, operation, endorsement);
                            break;
                        case RawActivationContent activation:
                            await ActivationsCommit.Apply(this, blockCommit.Block, operation, activation);
                            break;
                        case RawDoubleBakingEvidenceContent doubleBaking:
                            await DoubleBakingCommit.Apply(this, blockCommit.Block, operation, doubleBaking);
                            break;
                        case RawNonceRevelationContent revelation:
                            await NonceRevelationsCommit.Apply(this, blockCommit.Block, operation, revelation);
                            break;
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
                            throw new NotImplementedException($"'{content.GetType()}' is not implemented");
                    }
                }
            }

            await StateCommit.Apply(this, blockCommit.Block, rawBlock);
        }

        public override async Task Revert()
        {
            var currBlock = await Cache.GetCurrentBlockAsync();
            
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
                    case DoubleBakingOperation doubleBaking:
                        await DoubleBakingCommit.Revert(this, currBlock, doubleBaking);
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

            await DeactivationCommit.Revert(this, currBlock);
            await VotingCommit.Revert(this, currBlock);
            await FreezerCommit.Revert(this, currBlock);
            await BlockCommit.Revert(this, currBlock);
            await ProtoCommit.Revert(this, currBlock);

            await StateCommit.Revert(this, currBlock);
        }
    }
}
