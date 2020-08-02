using System.Collections.Generic;
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

        public InitiatorHandler(TezosNode node, TzktContext db, CacheService cache, QuotesService quotes, IConfiguration config, ILogger<InitiatorHandler> logger)
            : base(node, db, cache, quotes, config, logger)
        {
            Diagnostics = new Diagnostics();
            Serializer = new Serializer();
            Validator = new Validator(this);
        }

        public override async Task InitProtocol(IBlock block)
        {
            var stream = await Node.GetConstantsAsync(block.Level);
            var rawConst = await (Serializer as Serializer).DeserializeConstants(stream);

            var protocol = new Protocol
            {
                Hash = block.Protocol,
                Code = await Db.Protocols.CountAsync() - 1,
                FirstLevel = block.Level,
                LastLevel = block.Level,
                BlockDeposit = rawConst.BlockDeposit,
                BlockReward0 = rawConst.BlockReward,
                BlocksPerCommitment = rawConst.BlocksPerCommitment,
                BlocksPerCycle = rawConst.BlocksPerCycle,
                BlocksPerSnapshot = rawConst.BlocksPerSnapshot,
                BlocksPerVoting = rawConst.BlocksPerVoting,
                ByteCost = rawConst.ByteCost,
                EndorsementDeposit = rawConst.EndorsementDeposit,
                EndorsementReward0 = rawConst.EndorsementReward,
                EndorsersPerBlock = rawConst.EndorsersPerBlock,
                HardBlockGasLimit = rawConst.HardBlockGasLimit,
                HardOperationGasLimit = rawConst.HardOperationGasLimit,
                HardOperationStorageLimit = rawConst.HardOperationStorageLimit,
                OriginationSize = rawConst.OriginationBurn / rawConst.ByteCost,
                PreservedCycles = rawConst.PreservedCycles,
                RevelationReward = rawConst.RevelationReward,
                TimeBetweenBlocks = rawConst.TimeBetweenBlocks[0],
                TokensPerRoll = rawConst.TokensPerRoll
            };

            Db.Protocols.Add(protocol);
            Cache.Protocols.Add(protocol);
        }

        public override Task InitProtocol()
        {
            return Task.CompletedTask;
        }

        public override async Task Commit(IBlock block)
        {
            var rawBlock = block as RawBlock;

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
