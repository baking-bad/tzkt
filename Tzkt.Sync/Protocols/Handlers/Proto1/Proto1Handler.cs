using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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
        public override IRpc Rpc { get; }

        public Proto1Handler(TezosNode node, TzktContext db, CacheService cache, QuotesService quotes, IConfiguration config, ILogger<Proto1Handler> logger)
            : base(node, db, cache, quotes, config, logger)
        {
            Rpc = new Rpc(node);
            Diagnostics = new Diagnostics(db, Rpc);
            Validator = new Validator(this);
        }

        public override Task Migration() => Proto2.DelegatesMigration.Apply(this);

        public override Task CancelMigration() => Proto2.DelegatesMigration.Revert(this);

        public override async Task InitProtocol(JsonElement block)
        {
            var level = block.Required("header").RequiredInt32("level");
            var state = Cache.AppState.Get();
            var currProtocol = await Cache.Protocols.GetAsync(state.Protocol);

            Protocol protocol = null;
            if (state.Protocol != state.NextProtocol)
            {
                protocol = new Protocol
                {
                    Hash = block.RequiredString("protocol"),
                    Code = await Db.Protocols.CountAsync() - 1,
                    FirstLevel = level,
                    LastLevel = -1
                };
                Db.Protocols.Add(protocol);
                Cache.Protocols.Add(protocol);
            }
            else if (level % currProtocol.BlocksPerCycle == 1)
            {
                protocol = await Cache.Protocols.GetAsync(state.Protocol);
                Db.TryAttach(protocol);
            }

            if (protocol != null)
            {
                var rawConst = await Node.GetAsync($"chains/main/blocks/{level}/context/constants");

                protocol.BlockDeposit = rawConst.RequiredInt64("block_security_deposit");
                protocol.BlockReward0 = rawConst.RequiredInt64("block_reward");
                protocol.BlocksPerCommitment = rawConst.RequiredInt32("blocks_per_commitment");
                protocol.BlocksPerCycle = rawConst.RequiredInt32("blocks_per_cycle");
                protocol.BlocksPerSnapshot = rawConst.RequiredInt32("blocks_per_roll_snapshot");
                protocol.BlocksPerVoting = rawConst.RequiredInt32("blocks_per_voting_period");
                protocol.ByteCost = rawConst.RequiredInt32("cost_per_byte");
                protocol.EndorsementDeposit = rawConst.RequiredInt64("endorsement_security_deposit");
                protocol.EndorsementReward0 = rawConst.RequiredInt64("endorsement_reward");
                protocol.EndorsersPerBlock = rawConst.RequiredInt32("endorsers_per_block");
                protocol.HardBlockGasLimit = rawConst.RequiredInt32("hard_gas_limit_per_block");
                protocol.HardOperationGasLimit = rawConst.RequiredInt32("hard_gas_limit_per_operation");
                protocol.HardOperationStorageLimit = rawConst.RequiredInt32("hard_storage_limit_per_operation");
                protocol.OriginationSize = rawConst.RequiredInt32("origination_burn") / 1000;
                protocol.PreservedCycles = rawConst.RequiredInt32("preserved_cycles");
                protocol.RevelationReward = rawConst.RequiredInt64("seed_nonce_revelation_tip");
                protocol.TimeBetweenBlocks = rawConst.RequiredArray("time_between_blocks", 2)[0].ParseInt32();
                protocol.TokensPerRoll = rawConst.RequiredInt64("tokens_per_roll");
            }
        }

        public override async Task InitProtocol()
        {
            var state = Cache.AppState.Get();
            var currProtocol = await Cache.Protocols.GetAsync(state.Protocol);

            if (state.Protocol == state.NextProtocol &&
                state.Level % currProtocol.BlocksPerCycle != 0)
                return;
            
            var rawConst = await Node.GetAsync($"chains/main/blocks/{state.Level - 1}/context/constants");

            Db.TryAttach(currProtocol);

            currProtocol.BlockDeposit = rawConst.RequiredInt64("block_security_deposit");
            currProtocol.BlockReward0 = rawConst.RequiredInt64("block_reward");
            currProtocol.BlocksPerCommitment = rawConst.RequiredInt32("blocks_per_commitment");
            currProtocol.BlocksPerCycle = rawConst.RequiredInt32("blocks_per_cycle");
            currProtocol.BlocksPerSnapshot = rawConst.RequiredInt32("blocks_per_roll_snapshot");
            currProtocol.BlocksPerVoting = rawConst.RequiredInt32("blocks_per_voting_period");
            currProtocol.ByteCost = rawConst.RequiredInt32("cost_per_byte");
            currProtocol.EndorsementDeposit = rawConst.RequiredInt64("endorsement_security_deposit");
            currProtocol.EndorsementReward0 = rawConst.RequiredInt64("endorsement_reward");
            currProtocol.EndorsersPerBlock = rawConst.RequiredInt32("endorsers_per_block");
            currProtocol.HardBlockGasLimit = rawConst.RequiredInt32("hard_gas_limit_per_block");
            currProtocol.HardOperationGasLimit = rawConst.RequiredInt32("hard_gas_limit_per_operation");
            currProtocol.HardOperationStorageLimit = rawConst.RequiredInt32("hard_storage_limit_per_operation");
            currProtocol.OriginationSize = rawConst.RequiredInt32("origination_burn") / rawConst.RequiredInt32("cost_per_byte");
            currProtocol.PreservedCycles = rawConst.RequiredInt32("preserved_cycles");
            currProtocol.RevelationReward = rawConst.RequiredInt64("seed_nonce_revelation_tip");
            currProtocol.TimeBetweenBlocks = rawConst.RequiredArray("time_between_blocks", 2)[0].ParseInt32();
            currProtocol.TokensPerRoll = rawConst.RequiredInt64("tokens_per_roll");
        }

        public override async Task Commit(JsonElement block)
        {
            var blockCommit = new BlockCommit(this);
            await blockCommit.Apply(block);

            var freezerCommit = new FreezerCommit(this);
            await freezerCommit.Apply(blockCommit.Block, block);

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
                    throw new NotImplementedException($"'{content.GetType()}' is not implemented");
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
                        case "seed_nonce_revelation":
                            await new NonceRevelationsCommit(this).Apply(blockCommit.Block, operation, content);
                            break;
                        default:
                            throw new NotImplementedException($"'{content.RequiredString("kind")}' is not allowed in operations[2]");
                    }
                }
            }
            #endregion

            #region operations 3
            foreach (var operation in operations[3].EnumerateArray())
            {
                Cache.AppState.IncreaseManagerCounter(operation.RequiredArray("contents").Count());

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
                            await new OriginationsCommit(this).Apply(blockCommit.Block, operation, content);
                            break;
                        case "transaction":
                            var parent = new TransactionsCommit(this);
                            await parent.Apply(blockCommit.Block, operation, content);

                            if (content.Required("metadata").TryGetProperty("internal_operation_results", out var internalResult))
                            {
                                foreach (var internalContent in internalResult.EnumerateArray())
                                {
                                    switch (internalContent.RequiredString("kind"))
                                    {
                                        case "transaction":
                                            await new TransactionsCommit(this).ApplyInternal(blockCommit.Block, parent.Transaction, internalContent);
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
            }
            #endregion

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

            await new StateCommit(this).Apply(blockCommit.Block, block);
        }

        public override async Task AfterCommit(JsonElement rawBlock)
        {
            var block = await Cache.Blocks.CurrentAsync();
            await new SnapshotBalanceCommit(this).Apply(block);
        }

        public override async Task BeforeRevert()
        {
            var block = await Cache.Blocks.CurrentAsync();
            await new SnapshotBalanceCommit(this).Revert(block);
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

            await new StatisticsCommit(this).Revert(currBlock);

            await new BakerCycleCommit(this).Revert(currBlock);
            await new DelegatorCycleCommit(this).Revert(currBlock);
            await new CycleCommit(this).Revert(currBlock);
            await new BakingRightsCommit(this).Revert(currBlock);

            foreach (var operation in operations.OrderByDescending(x => x.Id))
            {
                switch (operation)
                {
                    case EndorsementOperation endorsement:
                        await new EndorsementsCommit(this).Revert(currBlock, endorsement);
                        break;
                    case ActivationOperation activation:
                        await new ActivationsCommit(this).Revert(currBlock, activation);
                        break;
                    case NonceRevelationOperation revelation:
                        await new NonceRevelationsCommit(this).Revert(currBlock, revelation);
                        break;
                    case RevealOperation reveal:
                        await new RevealsCommit(this).Revert(currBlock, reveal);
                        break;
                    case DelegationOperation delegation:
                        await new DelegationsCommit(this).Revert(currBlock, delegation);
                        break;
                    case OriginationOperation origination:
                        await new OriginationsCommit(this).Revert(currBlock, origination);
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

            await new FreezerCommit(this).Revert(currBlock);
            await new BlockCommit(this).Revert(currBlock);

            await new StateCommit(this).Revert(currBlock);
        }
    }
}
