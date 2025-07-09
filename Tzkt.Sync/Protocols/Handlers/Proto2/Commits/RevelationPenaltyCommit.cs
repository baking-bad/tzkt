using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto2
{
    class RevelationPenaltyCommit : ProtocolCommit
    {
        public RevelationPenaltyCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual async Task Apply(Block block, JsonElement rawBlock)
        {
            #region init
            List<RevelationPenaltyOperation>? revelationPenalties = null;

            if (block.Events.HasFlag(BlockEvents.CycleEnd))
            {
                if (HasPenaltiesUpdates(block, Context.Protocol, rawBlock))
                {
                    revelationPenalties = [];

                    var missedBlocks = await Db.Blocks
                        .Join(Db.Protocols, x => x.ProtoCode, x => x.Code, (block, protocol) => new { block, protocol })
                        .Where(x => x.block.Level % x.protocol.BlocksPerCommitment == 0 &&
                            x.block.Cycle == block.Cycle - 1 &&
                            x.block.RevelationId == null)
                        .Select(x => x.block)
                        .ToListAsync();

                    var penalizedBakers = missedBlocks
                        .Select(x => x.ProposerId)
                        .ToHashSet();

                    var bakerCycles = await Db.BakerCycles.AsNoTracking()
                        .Where(x => x.Cycle == block.Cycle - 1 && penalizedBakers.Contains(x.BakerId))
                        .ToListAsync();

                    var slashedBakers = bakerCycles
                        .Where(x => x.DoubleBakingLostStaked > 0 || x.DoubleConsensusLostStaked > 0)
                        .Select(x => x.BakerId)
                        .ToHashSet();

                    foreach (var missedBlock in missedBlocks)
                    {
                        var missedBlockProposer = Cache.Accounts.GetDelegate(missedBlock.ProposerId!.Value);
                        var slashed = slashedBakers.Contains(missedBlockProposer.Id);
                        revelationPenalties.Add(new RevelationPenaltyOperation
                        {
                            Id = Cache.AppState.NextOperationId(),
                            BakerId = missedBlockProposer.Id,
                            Level = block.Level,
                            Timestamp = block.Timestamp,
                            MissedLevel = missedBlock.Level,
                            Loss = slashed ? 0 : missedBlock.RewardDelegated + missedBlock.Fees
                        });
                    }
                }
            }
            #endregion

            if (revelationPenalties == null) return;

            foreach (var penalty in revelationPenalties)
            {
                #region entities
                var delegat = Cache.Accounts.GetDelegate(penalty.BakerId);
                Db.TryAttach(delegat);
                #endregion

                delegat.Balance -= penalty.Loss;
                delegat.StakingBalance -= penalty.Loss > 0
                    ? (await Cache.Blocks.GetAsync(penalty.MissedLevel)).Fees
                    : 0;

                delegat.RevelationPenaltiesCount++;
                block.Operations |= Operations.RevelationPenalty;

                Cache.AppState.Get().RevelationPenaltyOpsCount++;
                Cache.Statistics.Current.TotalBurned += penalty.Loss;
                Cache.Statistics.Current.TotalFrozen -= penalty.Loss;

                Db.RevelationPenaltyOps.Add(penalty);
                Context.RevelationPenaltyOps.Add(penalty);
            }
        }

        public virtual async Task Revert(Block block)
        {
            foreach (var penalty in Context.RevelationPenaltyOps)
            {
                #region entities
                var delegat = Cache.Accounts.GetDelegate(penalty.BakerId);
                Db.TryAttach(delegat);
                #endregion

                delegat.Balance += penalty.Loss;
                delegat.StakingBalance += penalty.Loss > 0
                    ? (await Cache.Blocks.GetAsync(penalty.MissedLevel)).Fees
                    : 0;

                delegat.RevelationPenaltiesCount--;

                Cache.AppState.Get().RevelationPenaltyOpsCount--;

                Db.RevelationPenaltyOps.Remove(penalty);
                Cache.AppState.ReleaseOperationId();
            }
        }

        protected virtual int GetFreezerCycle(JsonElement el) => el.RequiredInt32("level");

        protected virtual bool HasPenaltiesUpdates(Block block, Protocol protocol, JsonElement rawBlock)
        {
            return rawBlock
                .Required("metadata")
                .RequiredArray("balance_updates")
                .EnumerateArray()
                .Any(x => x.RequiredString("kind")[0] == 'f' &&
                          x.RequiredInt64("change") < 0 &&
                          GetFreezerCycle(x) != block.Cycle - protocol.ConsensusRightsDelay);
        }
    }
}
