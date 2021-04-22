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
            List<RevelationPenaltyOperation> revelationPanlties = null;

            if (block.Events.HasFlag(BlockEvents.CycleEnd))
            {
                var cycle = (block.Level - 1) / block.Protocol.BlocksPerCycle;

                if (HasPanltiesUpdates(block, rawBlock))
                {
                    revelationPanlties = new List<RevelationPenaltyOperation>();

                    var missedBlocks = await Db.Blocks
                        .Include(x => x.Baker)
                        .Where(x => x.Level % block.Protocol.BlocksPerCommitment == 0 &&
                            (x.Level - 1) / block.Protocol.BlocksPerCycle == cycle - 1 &&
                            x.RevelationId == null)
                        .ToListAsync();

                    foreach (var missedBlock in missedBlocks)
                    {
                        Cache.Accounts.Add(missedBlock.Baker);
                        revelationPanlties.Add(new RevelationPenaltyOperation
                        {
                            Id = Cache.AppState.NextOperationId(),
                            Baker = missedBlock.Baker,
                            Block = block,
                            Level = block.Level,
                            Timestamp = block.Timestamp,
                            MissedLevel = missedBlock.Level,
                            LostReward = missedBlock.Reward,
                            LostFees = missedBlock.Fees
                        });
                    }
                }
            }
            #endregion

            if (revelationPanlties == null) return;

            foreach (var penalty in revelationPanlties)
            {
                #region entities
                //var block = penalty.Block;
                var delegat = penalty.Baker;

                Db.TryAttach(delegat);
                #endregion

                delegat.Balance -= penalty.LostReward;
                delegat.Balance -= penalty.LostFees;

                delegat.FrozenRewards -= penalty.LostReward;
                delegat.FrozenFees -= penalty.LostFees;

                delegat.StakingBalance -= penalty.LostFees;

                delegat.RevelationPenaltiesCount++;
                block.Operations |= Operations.RevelationPenalty;

                Db.RevelationPenaltyOps.Add(penalty);
            }
        }

        public virtual Task Revert(Block block)
        {
            #region init
            List<RevelationPenaltyOperation> revelationPanlties = null;

            if (block.RevelationPenalties?.Count > 0)
            {
                revelationPanlties = block.RevelationPenalties;
                foreach (var penalty in revelationPanlties)
                {
                    penalty.Block ??= block;
                    penalty.Baker ??= Cache.Accounts.GetDelegate(penalty.BakerId);
                }
            }
            #endregion

            if (revelationPanlties == null) return Task.CompletedTask;

            foreach (var penalty in revelationPanlties)
            {
                #region entities
                //var block = penalty.Block;
                var delegat = penalty.Baker;

                Db.TryAttach(delegat);
                #endregion

                delegat.Balance += penalty.LostReward;
                delegat.Balance += penalty.LostFees;

                delegat.FrozenRewards += penalty.LostReward;
                delegat.FrozenFees += penalty.LostFees;

                delegat.StakingBalance += penalty.LostFees;

                delegat.RevelationPenaltiesCount--;

                Db.RevelationPenaltyOps.Remove(penalty);
            }

            return Task.CompletedTask;
        }

        protected virtual int GetFreezerCycle(JsonElement el) => el.RequiredInt32("level");

        protected virtual bool HasPanltiesUpdates(Block block, JsonElement rawBlock)
        {
            var cycle = (block.Level - 1) / block.Protocol.BlocksPerCycle;
            return rawBlock
                .Required("metadata")
                .RequiredArray("balance_updates")
                .EnumerateArray()
                .Skip(cycle < block.Protocol.NoRewardCycles ? 2 : 3)
                .Any(x => x.RequiredString("kind")[0] == 'f' && GetFreezerCycle(x) != cycle - block.Protocol.PreservedCycles);
        }
    }
}
