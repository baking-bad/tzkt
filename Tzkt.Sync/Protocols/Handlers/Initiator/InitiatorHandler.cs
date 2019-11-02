using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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
        public override ISerializer Serializer { get; }
        public override IValidator Validator { get; }

        public InitiatorHandler(TezosNode node, TzktContext db, CacheService cache, DiagnosticService diagnostics, ILogger<InitiatorHandler> logger)
            : base(node, db, cache, diagnostics, logger)
        {
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
                BlockDeposit = rawConst.BlockDeposit,
                BlockReward = rawConst.BlockReward,
                BlocksPerCommitment = rawConst.BlocksPerCommitment,
                BlocksPerCycle = rawConst.BlocksPerCycle,
                BlocksPerSnapshot = rawConst.BlocksPerSnapshot,
                BlocksPerVoting = rawConst.BlocksPerVoting,
                ByteCost = rawConst.ByteCost,
                EndorsementDeposit = rawConst.EndorsementDeposit,
                EndorsementReward = rawConst.EndorsementReward,
                EndorsersPerBlock = rawConst.EndorsersPerBlock,
                HardBlockGasLimit = rawConst.HardBlockGasLimit,
                HardOperationGasLimit = rawConst.HardOperationGasLimit,
                HardOperationStorageLimit = rawConst.HardOperationStorageLimit,
                OriginationSize = rawConst.OriginationBurn / rawConst.ByteCost,
                PreserverCycles = rawConst.PreserverCycles,
                RevelationReward = rawConst.RevelationReward,
                TimeBetweenBlocks = rawConst.TimeBetweenBlocks[0],
                TokensPerRoll = rawConst.TokensPerRoll
            };

            Db.Protocols.Add(protocol);
            Cache.AddProtocol(protocol);
        }

        public override Task InitProtocol()
        {
            return Task.CompletedTask;
        }

        public override async Task Commit(IBlock block)
        {
            var rawBlock = block as RawBlock;

            await BootstrapCommit.Apply(this, rawBlock);

            var blockCommit = await BlockCommit.Apply(this, rawBlock);
            await VotingCommit.Apply(this, rawBlock);

            await StateCommit.Apply(this, blockCommit.Block, rawBlock);
        }

        public override async Task Revert()
        {
            var currBlock = await Cache.GetCurrentBlockAsync();

            await VotingCommit.Revert(this, currBlock);
            await BlockCommit.Revert(this, currBlock);

            await BootstrapCommit.Revert(this, currBlock);

            await StateCommit.Revert(this, currBlock);
        }

        /*private async Task InitCycle(int cycle)
        {
            #region init rights
            var rights = await Task.WhenAll(
                Node.GetBakingRightsAsync(1, cycle, 1),
                Node.GetEndorsingRightsAsync(1, cycle));

            var bakingRights = rights[0]
                .Select(x => new BakingRight
                {
                    Baker = GetContract(x["delegate"].String()),
                    Level = x["level"].Int32(),
                    Priority = x["priority"].Int32()
                });

            var endorsingRights = rights[1]
                .Select(x => new EndorsingRight
                {
                    Baker = GetContract(x["delegate"].String()),
                    Level = x["level"].Int32(),
                    Slots = x["slots"].Count()
                });

            Db.BakingRights.AddRange(bakingRights);
            Db.EndorsingRights.AddRange(endorsingRights);
            #endregion

            #region init cycle
            var cycleObj = new Cycle
            {
                Index = cycle,
                Snapshot = 1,
            };
            Db.Cycles.Add(cycleObj);
            #endregion

            #region init snapshots
            var snapshots = Contracts.Values
                .Where(x => x.Staked)
                .Select(x => new BalanceSnapshot
                {
                    Balance = x.Balance,
                    Address = x,
                    Delegate = GetContract(x.Delegate?.Address ?? x.Address),
                    Level = cycleObj.Snapshot
                });
            #endregion

            #region init delegators
            var delegators = snapshots
                .Where(x => x.Contract.Kind != ContractKind.Baker)
                .Select(x => new DelegatorSnapshot
                {
                    Baker = x.Delegate,
                    Balance = x.Balance,
                    Delegator = x.Contract,
                    Cycle = cycle
                });
            Db.DelegatorSnapshots.AddRange(delegators);
            #endregion

            #region init bakers
            var bakers = snapshots
                .Where(x => x.Contract.Kind == ContractKind.Baker)
                .Select(x => new BakingCycle
                {
                    Baker = x.Contract,
                    Balance = x.Balance,
                    Cycle = cycle,
                    StakingBalance = snapshots
                        .Where(s => s.Delegate == x.Contract)
                        .Sum(s => s.Balance),
                    Blocks = bakingRights
                        .Count(r => r.Priority == 0 && r.Baker == x.Contract),
                    Endorsements = endorsingRights
                        .Where(r => r.Baker == x.Contract)
                        .DefaultIfEmpty(new EndorsingRight())
                        .Sum(r => r.Slots)
                });
            Db.BakerCycles.AddRange(bakers);
            #endregion
        }*/
        /*private async Task ClearCycle(int cycle)
        {
            Db.BakingRights.RemoveRange(
                await Db.BakingRights.Where(x => (x.Level - 1) / 4096 == cycle).ToListAsync());

            Db.EndorsingRights.RemoveRange(
                await Db.EndorsingRights.Where(x => (x.Level - 1) / 4096 == cycle).ToListAsync());

            Db.Cycles.Remove(
                await Db.Cycles.FirstAsync(x => x.Index == cycle));

            Db.DelegatorSnapshots.RemoveRange(
                await Db.DelegatorSnapshots.Where(x => x.Cycle == cycle).ToListAsync());

            Db.BakerCycles.RemoveRange(
                await Db.BakerCycles.Where(x => x.Cycle == cycle).ToListAsync());
        }*/
    }
}
