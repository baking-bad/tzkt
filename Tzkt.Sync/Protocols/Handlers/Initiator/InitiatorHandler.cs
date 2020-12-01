using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Tzkt.Data;
using Tzkt.Data.Models;
using Tzkt.Sync.Services;
using Tzkt.Sync.Protocols.Initiator;

namespace Tzkt.Sync.Protocols
{
    class InitiatorHandler : ProtocolHandler
    {
        public override string Protocol => "Initiator";
        public override IDiagnostics Diagnostics { get; }
        public override ISerializer Serializer { get; }
        public override IValidator Validator { get; }
        public override IRpc Rpc { get; }

        public InitiatorHandler(TezosNode node, TzktContext db, CacheService cache, QuotesService quotes, IConfiguration config, ILogger<InitiatorHandler> logger)
            : base(node, db, cache, quotes, config, logger)
        {
            Diagnostics = new Diagnostics();
            Serializer = new Serializer();
            Validator = new Validator(this);
            Rpc = new Rpc(node);
        }

        public override Task Precache(JsonElement block) => Task.CompletedTask;

        public override async Task InitProtocol(JsonElement block)
        {
            var level = block.Required("header").RequiredInt32("level");
            var rawConst = await Node.GetAsync($"chains/main/blocks/{level}/context/constants");

            var protocol = new Protocol
            {
                Hash = block.RequiredString("protocol"),
                Code = await Db.Protocols.CountAsync() - 1,
                FirstLevel = level,
                LastLevel = level,
                BlockDeposit = rawConst.RequiredInt64("block_security_deposit"),
                BlockReward0 = rawConst.RequiredInt64("block_reward"),
                BlocksPerCommitment = rawConst.RequiredInt32("blocks_per_commitment"),
                BlocksPerCycle = rawConst.RequiredInt32("blocks_per_cycle"),
                BlocksPerSnapshot = rawConst.RequiredInt32("blocks_per_roll_snapshot"),
                BlocksPerVoting = rawConst.RequiredInt32("blocks_per_voting_period"),
                ByteCost = rawConst.RequiredInt32("cost_per_byte"),
                EndorsementDeposit = rawConst.RequiredInt64("endorsement_security_deposit"),
                EndorsementReward0 = rawConst.RequiredInt64("endorsement_reward"),
                EndorsersPerBlock = rawConst.RequiredInt32("endorsers_per_block"),
                HardBlockGasLimit = rawConst.RequiredInt32("hard_gas_limit_per_block"),
                HardOperationGasLimit = rawConst.RequiredInt32("hard_gas_limit_per_operation"),
                HardOperationStorageLimit = rawConst.RequiredInt32("hard_storage_limit_per_operation"),
                OriginationSize = rawConst.RequiredInt32("origination_burn") / 1000,
                PreservedCycles = rawConst.RequiredInt32("preserved_cycles"),
                RevelationReward = rawConst.RequiredInt64("seed_nonce_revelation_tip"),
                TimeBetweenBlocks = rawConst.RequiredArray("time_between_blocks", 2)[0].ParseInt32(),
                TokensPerRoll = rawConst.RequiredInt64("tokens_per_roll")
            };

            Db.Protocols.Add(protocol);
            Cache.Protocols.Add(protocol);
        }

        public override Task InitProtocol()
        {
            return Task.CompletedTask;
        }

        public override async Task Commit(JsonElement block)
        {
            var rawBlock = JsonSerializer.Deserialize<RawBlock>(block.GetRawText(), Initiator.Serializer.Options);

            var blockCommit = await BlockCommit.Apply(this, rawBlock);
            var bootstrapCommit = await BootstrapCommit.Apply(this, blockCommit.Block, rawBlock);
            await VotingCommit.Apply(this, rawBlock);

            var brCommit = await BakingRightsCommit.Apply(this, blockCommit.Block, bootstrapCommit.BootstrapedAccounts);
            await CycleCommit.Apply(this, blockCommit.Block, bootstrapCommit.BootstrapedAccounts);
            await DelegatorCycleCommit.Apply(this, blockCommit.Block, bootstrapCommit.BootstrapedAccounts);
            
            await BakerCycleCommit.Apply(this,
                blockCommit.Block,
                bootstrapCommit.BootstrapedAccounts,
                brCommit.FutureBakingRights,
                brCommit.FutureEndorsingRights);

            await StatisticsCommit.Apply(this, blockCommit.Block, bootstrapCommit.BootstrapedAccounts, bootstrapCommit.Commitments);

            await StateCommit.Apply(this, blockCommit.Block, rawBlock);
        }

        public override async Task AfterCommit(JsonElement rawBlock)
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

            await StatisticsCommit.Revert(this);

            await BakerCycleCommit.Revert(this);
            await DelegatorCycleCommit.Revert(this);
            await CycleCommit.Revert(this);
            await BakingRightsCommit.Revert(this);

            await VotingCommit.Revert(this, currBlock);
            await BootstrapCommit.Revert(this, currBlock);
            await BlockCommit.Revert(this, currBlock);

            await StateCommit.Revert(this, currBlock);
        }
    }
}
