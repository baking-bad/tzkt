using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
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
            List<RevelationPenaltyOperation> revelationPenalties = null;

            if (block.Events.HasFlag(BlockEvents.CycleEnd))
            {
                if (HasPenaltiesUpdates(block, rawBlock))
                {
                    revelationPenalties = new List<RevelationPenaltyOperation>();

                    var missedBlocks = await Db.Blocks
                        .Include(x => x.Proposer)
                        .Include(x => x.Protocol)
                        .Where(x => x.Level % x.Protocol.BlocksPerCommitment == 0 &&
                            x.Cycle == block.Cycle - 1 &&
                            x.RevelationId == null)
                        .ToListAsync();

                    var penalizedBakers = missedBlocks
                        .Select(x => x.ProposerId)
                        .ToHashSet();

                    var bakerCycles = await Db.BakerCycles.AsNoTracking()
                        .Where(x => x.Cycle == block.Cycle - 1 && penalizedBakers.Contains(x.BakerId))
                        .ToListAsync();

                    var slashedBakers = bakerCycles
                        .Where(x => x.DoubleBakingLosses > 0 || x.DoubleEndorsingLosses > 0)
                        .Select(x => x.BakerId)
                        .ToHashSet();

                    foreach (var missedBlock in missedBlocks)
                    {
                        Cache.Accounts.Add(missedBlock.Proposer);
                        var slashed = slashedBakers.Contains((int)missedBlock.ProposerId);
                        revelationPenalties.Add(new RevelationPenaltyOperation
                        {
                            Id = Cache.AppState.NextOperationId(),
                            Baker = missedBlock.Proposer,
                            Block = block,
                            Level = block.Level,
                            Timestamp = block.Timestamp,
                            MissedLevel = missedBlock.Level,
                            Loss = slashed ? 0 : missedBlock.Reward + missedBlock.Fees
                        });
                    }
                }
            }
            #endregion

            if (revelationPenalties == null) return;

            foreach (var penalty in revelationPenalties)
            {
                #region entities
                //var block = penalty.Block;
                var delegat = penalty.Baker;

                Db.TryAttach(delegat);
                #endregion

                delegat.Balance -= penalty.Loss;
                delegat.StakingBalance -= penalty.Loss > 0
                    ? (await Cache.Blocks.GetAsync(penalty.MissedLevel)).Fees
                    : 0;

                delegat.RevelationPenaltiesCount++;
                block.Operations |= Operations.RevelationPenalty;

                Db.RevelationPenaltyOps.Add(penalty);
            }
        }

        public virtual async Task Revert(Block block)
        {
            #region init
            List<RevelationPenaltyOperation> revelationPenalties = null;

            if (block.RevelationPenalties?.Count > 0)
            {
                revelationPenalties = block.RevelationPenalties;
                foreach (var penalty in revelationPenalties)
                {
                    penalty.Block ??= block;
                    penalty.Baker ??= Cache.Accounts.GetDelegate(penalty.BakerId);
                }
            }
            #endregion

            if (revelationPenalties == null) return;

            foreach (var penalty in revelationPenalties)
            {
                #region entities
                //var block = penalty.Block;
                var delegat = penalty.Baker;

                Db.TryAttach(delegat);
                #endregion

                delegat.Balance += penalty.Loss;
                delegat.StakingBalance += penalty.Loss > 0
                    ? (await Cache.Blocks.GetAsync(penalty.MissedLevel)).Fees
                    : 0;

                delegat.RevelationPenaltiesCount--;

                Db.RevelationPenaltyOps.Remove(penalty);
                Cache.AppState.ReleaseOperationId();
            }
        }

        protected virtual int GetFreezerCycle(JsonElement el) => el.RequiredInt32("level");

        protected virtual bool HasPenaltiesUpdates(Block block, JsonElement rawBlock)
        {
            return rawBlock
                .Required("metadata")
                .RequiredArray("balance_updates")
                .EnumerateArray()
                .Any(x => x.RequiredString("kind")[0] == 'f' &&
                          x.RequiredInt64("change") < 0 &&
                          GetFreezerCycle(x) != block.Cycle - block.Protocol.PreservedCycles);
        }
    }
}
