using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto6
{
    class RevelationPenaltyCommit : ProtocolCommit
    {
        public List<RevelationPenaltyOperation> RevelationPanlties { get; private set; }

        RevelationPenaltyCommit(ProtocolHandler protocol) : base(protocol) { }

        public async Task Init(Block block, RawBlock rawBlock)
        {
            if (block.Events.HasFlag(BlockEvents.CycleEnd))
            {
                var protocol = await Cache.GetProtocolAsync(rawBlock.Protocol);
                var cycle = (rawBlock.Level - 1) / protocol.BlocksPerCycle;
                
                if (rawBlock.Metadata.BalanceUpdates.Skip(protocol.BlockReward0 == 0 || rawBlock.Operations[0].Count == 0 ? 2 : 3)
                    .Any(x => x is FreezerUpdate fu && fu.Cycle != cycle - protocol.PreservedCycles))
                {
                    RevelationPanlties = new List<RevelationPenaltyOperation>();

                    var missedBlocks = await Db.Blocks
                        .Include(x => x.Baker)
                        .Where(x => x.Level % protocol.BlocksPerCommitment == 0 &&
                            (x.Level - 1) / protocol.BlocksPerCycle == cycle - 1 &&
                            x.RevelationId == null)
                        .ToListAsync();

                    foreach (var missedBlock in missedBlocks)
                    {
                        Cache.AddAccount(missedBlock.Baker);
                        RevelationPanlties.Add(new RevelationPenaltyOperation
                        {
                            Id = await Cache.NextCounterAsync(),
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
        }

        public async Task Init(Block block)
        {
            if (block.RevelationPenalties?.Count > 0)
            {
                RevelationPanlties = block.RevelationPenalties;
                foreach (var penalty in RevelationPanlties)
                {
                    penalty.Block ??= block;
                    penalty.Baker ??= (Data.Models.Delegate)await Cache.GetAccountAsync(penalty.BakerId);
                }
            }
        }

        public override Task Apply()
        {
            if (RevelationPanlties == null) return Task.CompletedTask;

            foreach (var penalty in RevelationPanlties)
            {
                #region entities
                var block = penalty.Block;
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

            return Task.CompletedTask;
        }

        public override Task Revert()
        {
            if (RevelationPanlties == null) return Task.CompletedTask;

            foreach (var penalty in RevelationPanlties)
            {
                #region entities
                var block = penalty.Block;
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

        #region static
        public static async Task<RevelationPenaltyCommit> Apply(ProtocolHandler proto, Block block, RawBlock rawBlock)
        {
            var commit = new RevelationPenaltyCommit(proto);
            await commit.Init(block, rawBlock);
            await commit.Apply();

            return commit;
        }

        public static async Task<RevelationPenaltyCommit> Revert(ProtocolHandler proto, Block block)
        {
            var commit = new RevelationPenaltyCommit(proto);
            await commit.Init(block);
            await commit.Revert();

            return commit;
        }
        #endregion
    }
}
