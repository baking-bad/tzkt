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
using Tzkt.Sync.Protocols.Proto3;

namespace Tzkt.Sync.Protocols
{
    class Proto3Handler : ProtocolHandler
    {
        public override string Protocol => "Proto 3";
        public override IDiagnostics Diagnostics { get; }
        public override ISerializer Serializer { get; }
        public override IValidator Validator { get; }

        public Proto3Handler(TezosNode node, TzktContext db, CacheService cache, IConfiguration config, ILogger<Proto3Handler> logger)
            : base(node, db, cache, config, logger)
        {
            Diagnostics = new Diagnostics(db, node);
            Serializer = new Serializer();
            Validator = new Validator(this);
        }

        public override Task Migration() => Proto4.InvoiceMigration.Apply(this);

        public override Task CancelMigration() => Proto4.InvoiceMigration.Revert(this);

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
                #region update constants
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
                protocol.OriginationSize = rawConst.OriginationSize;
                protocol.PreservedCycles = rawConst.PreservedCycles;
                protocol.RevelationReward = rawConst.RevelationReward;
                protocol.TimeBetweenBlocks = rawConst.TimeBetweenBlocks[0];
                protocol.TokensPerRoll = rawConst.TokensPerRoll;
                #endregion
            }
        }

        public override async Task InitProtocol()
        {
            var state = Cache.AppState.Get();
            var currProtocol = await Cache.Protocols.GetAsync(state.Protocol);

            if (state.Protocol == state.NextProtocol &&
                state.Level % currProtocol.BlocksPerCycle != 0)
                return;

            Db.TryAttach(currProtocol);

            #region update constants
            var stream = await Node.GetConstantsAsync(state.Level - 1);
            var rawConst = await (Serializer as Serializer).DeserializeConstants(stream);

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
            currProtocol.OriginationSize = rawConst.OriginationSize;
            currProtocol.PreservedCycles = rawConst.PreservedCycles;
            currProtocol.RevelationReward = rawConst.RevelationReward;
            currProtocol.TimeBetweenBlocks = rawConst.TimeBetweenBlocks[0];
            currProtocol.TokensPerRoll = rawConst.TokensPerRoll;
            #endregion
        }

        public override async Task Commit(IBlock block)
        {
            var rawBlock = block as RawBlock;

            var blockCommit = await BlockCommit.Apply(this, rawBlock);
            await VotingCommit.Apply(this, blockCommit.Block, rawBlock);
            await FreezerCommit.Apply(this, blockCommit.Block, rawBlock);
            await RevelationPenaltyCommit.Apply(this, blockCommit.Block, rawBlock);
            await DeactivationCommit.Apply(this, blockCommit.Block, rawBlock);

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
                            throw new NotImplementedException($"'{content.GetType()}' is not expected in operations[0]");
                    }
                }
            }
            #endregion

            #region operations 1
            foreach (var operation in rawBlock.Operations[1])
            {
                foreach (var content in operation.Contents)
                {
                    if (content is RawProposalContent proposal)
                        await ProposalsCommit.Apply(this, blockCommit.Block, operation, proposal);
                    else if (content is RawBallotContent ballot)
                        await BallotsCommit.Apply(this, blockCommit.Block, operation, ballot);
                    else 
                        throw new NotImplementedException($"'{content.GetType()}' is not expected in operations[1]");
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
                        case RawDoubleBakingEvidenceContent doubleBaking:
                            await DoubleBakingCommit.Apply(this, blockCommit.Block, operation, doubleBaking);
                            break;
                        case RawNonceRevelationContent revelation:
                            await NonceRevelationsCommit.Apply(this, blockCommit.Block, operation, revelation);
                            break;
                        default:
                            throw new NotImplementedException($"'{content.GetType()}' is not expected in operations[2]");
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
            await SnapshotBalanceCommit.Apply(this, rawBlock, block);
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

            if (currBlock.Operations.HasFlag(Operations.DoubleBakings))
                query = query.Include(x => x.DoubleBakings);

            if (currBlock.Operations.HasFlag(Operations.Proposals))
                query = query.Include(x => x.Proposals);

            if (currBlock.Operations.HasFlag(Operations.Ballots))
                query = query.Include(x => x.Ballots);

            if (currBlock.Operations.HasFlag(Operations.RevelationPenalty))
                query = query.Include(x => x.RevelationPenalties);

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

            if (currBlock.DoubleBakings != null)
                operations.AddRange(currBlock.DoubleBakings);

            if (currBlock.Proposals != null)
                operations.AddRange(currBlock.Proposals);

            if (currBlock.Ballots != null)
                operations.AddRange(currBlock.Ballots);

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
                    case ProposalOperation proposal:
                        await ProposalsCommit.Revert(this, currBlock, proposal);
                        break;
                    case BallotOperation ballot:
                        await BallotsCommit.Revert(this, currBlock, ballot);
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
            await RevelationPenaltyCommit.Revert(this, currBlock);
            await FreezerCommit.Revert(this, currBlock);
            await VotingCommit.Revert(this, currBlock);
            await BlockCommit.Revert(this, currBlock);

            await StateCommit.Revert(this, currBlock);
        }
    }
}
