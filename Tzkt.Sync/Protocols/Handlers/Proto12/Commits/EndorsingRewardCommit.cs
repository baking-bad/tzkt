using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto12
{
    class EndorsingRewardCommit : ProtocolCommit
    {
        public List<EndorsingRewardOperation> Ops { get; set; }

        public EndorsingRewardCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual async Task Apply(Block block, JsonElement rawBlock)
        {
            if (!block.Events.HasFlag(BlockEvents.CycleEnd))
                return;

            var losses = rawBlock.Required("metadata").RequiredArray("balance_updates").EnumerateArray()
                .Where(x => x.RequiredString("origin") == "block" &&
                            x.RequiredString("kind") == "burned" &&
                            x.RequiredString("category") == "lost endorsing rewards")
                .ToDictionary(x => Cache.Accounts.GetDelegate(x.RequiredString("delegate")).Id, x => x.RequiredInt64("change"));

            var bakerCycles = await Cache.BakerCycles.GetAsync(block.Cycle);
            Ops = new(bakerCycles.Count);

            foreach (var (bakerId, bakerCycle) in bakerCycles.Where(x => x.Value.FutureEndorsementRewards > 0))
            {
                Ops.Add(new()
                {
                    Id = Cache.AppState.NextOperationId(),
                    BakerId = bakerId,
                    Level = block.Level,
                    Timestamp = block.Timestamp,
                    Expected = bakerCycle.FutureEndorsementRewards,
                    RewardLiquid = bakerCycle.FutureEndorsementRewards
                });

                Db.TryAttach(bakerCycle);
                if (losses.TryGetValue(bakerId, out var loss))
                {
                    if (bakerCycle.FutureEndorsementRewards != loss)
                        throw new Exception("FutureEndorsementRewards != loss");

                    Ops[^1].RewardLiquid = 0; 
                    bakerCycle.MissedEndorsementRewards += bakerCycle.FutureEndorsementRewards;
                    bakerCycle.FutureEndorsementRewards = 0;
                }
                else
                {
                    bakerCycle.EndorsementRewardsLiquid += bakerCycle.FutureEndorsementRewards;
                    bakerCycle.FutureEndorsementRewards = 0;
                }
            }

            foreach (var op in Ops)
            {
                var baker = Cache.Accounts.GetDelegate(op.BakerId);
                Db.TryAttach(baker);

                baker.Balance += op.RewardLiquid;
                baker.StakingBalance += op.RewardLiquid;
                baker.EndorsingRewardsCount++;

                block.Operations |= Operations.EndorsingRewards;

                Cache.Statistics.Current.TotalCreated += op.RewardLiquid;
            }

            Cache.AppState.Get().EndorsingRewardOpsCount += Ops.Count;

            Db.EndorsingRewardOps.AddRange(Ops);
        }

        public virtual async Task Revert(Block block)
        {
            if (!block.Operations.HasFlag(Operations.EndorsingRewards))
                return;

            var ops = await Db.EndorsingRewardOps.Where(x => x.Level == block.Level).ToListAsync();

            foreach (var op in ops)
            {
                var baker = Cache.Accounts.GetDelegate(op.BakerId);
                Db.TryAttach(baker);

                baker.Balance -= op.RewardLiquid;
                baker.StakingBalance -= op.RewardLiquid;
                baker.EndorsingRewardsCount--;

                var bakerCycle = await Cache.BakerCycles.GetAsync(block.Cycle, baker.Id);
                Db.TryAttach(bakerCycle);

                bakerCycle.FutureEndorsementRewards = op.Expected;
                if (op.Expected == op.RewardLiquid)
                    bakerCycle.EndorsementRewardsLiquid -= op.Expected;
                else
                    bakerCycle.MissedEndorsementRewards -= op.Expected;
            }

            Cache.AppState.Get().EndorsingRewardOpsCount -= ops.Count;

            Db.EndorsingRewardOps.RemoveRange(ops);
            Cache.AppState.ReleaseOperationId(ops.Count);
        }
    }
}
