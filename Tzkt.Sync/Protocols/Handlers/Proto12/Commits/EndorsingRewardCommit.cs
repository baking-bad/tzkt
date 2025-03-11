using System.Text.Json;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto12
{
    class EndorsingRewardCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public virtual async Task Apply(Block block, JsonElement rawBlock)
        {
            if (!block.Events.HasFlag(BlockEvents.CycleEnd))
                return;

            var losses = rawBlock.Required("metadata").RequiredArray("balance_updates").EnumerateArray()
                .Where(x => x.RequiredString("origin") == "block" &&
                            x.RequiredString("kind") == "burned" &&
                            x.RequiredString("category") == "lost endorsing rewards")
                .ToDictionary(x => Cache.Accounts.GetExistingDelegate(x.RequiredString("delegate")).Id, x => x.RequiredInt64("change"));

            var bakerCycles = await Cache.BakerCycles.GetAsync(block.Cycle);
            var ops = new List<EndorsingRewardOperation>(bakerCycles.Count);

            foreach (var (bakerId, bakerCycle) in bakerCycles.Where(x => x.Value.FutureEndorsementRewards > 0))
            {
                ops.Add(new()
                {
                    Id = Cache.AppState.NextOperationId(),
                    BakerId = bakerId,
                    Level = block.Level,
                    Timestamp = block.Timestamp,
                    Expected = bakerCycle.FutureEndorsementRewards,
                    RewardDelegated = bakerCycle.FutureEndorsementRewards
                });

                Db.TryAttach(bakerCycle);
                if (losses.TryGetValue(bakerId, out var loss))
                {
                    if (bakerCycle.FutureEndorsementRewards != loss)
                        throw new Exception("FutureEndorsementRewards != loss");

                    ops[^1].RewardDelegated = 0; 
                    bakerCycle.MissedEndorsementRewards += bakerCycle.FutureEndorsementRewards;
                    bakerCycle.FutureEndorsementRewards = 0;
                }
                else
                {
                    bakerCycle.EndorsementRewardsDelegated += bakerCycle.FutureEndorsementRewards;
                    bakerCycle.FutureEndorsementRewards = 0;
                }
            }

            foreach (var op in ops)
            {
                var baker = Cache.Accounts.GetDelegate(op.BakerId);
                Db.TryAttach(baker);

                baker.Balance += op.RewardDelegated;
                baker.StakingBalance += op.RewardDelegated;
                baker.EndorsingRewardsCount++;

                block.Operations |= Operations.EndorsingRewards;

                Cache.Statistics.Current.TotalCreated += op.RewardDelegated;
            }

            Cache.AppState.Get().EndorsingRewardOpsCount += ops.Count;

            Db.EndorsingRewardOps.AddRange(ops);
            Context.EndorsingRewardOps.AddRange(ops);
        }

        public virtual async Task Revert(Block block)
        {
            if (Context.EndorsingRewardOps.Count == 0)
                return;

            foreach (var op in Context.EndorsingRewardOps)
            {
                var baker = Cache.Accounts.GetDelegate(op.BakerId);
                Db.TryAttach(baker);

                baker.Balance -= op.RewardDelegated;
                baker.StakingBalance -= op.RewardDelegated;
                baker.EndorsingRewardsCount--;

                var bakerCycle = await Cache.BakerCycles.GetAsync(block.Cycle, baker.Id);
                Db.TryAttach(bakerCycle);

                bakerCycle.FutureEndorsementRewards = op.Expected;
                if (op.Expected == op.RewardDelegated)
                    bakerCycle.EndorsementRewardsDelegated -= op.Expected;
                else
                    bakerCycle.MissedEndorsementRewards -= op.Expected;
            }

            Cache.AppState.Get().EndorsingRewardOpsCount -= Context.EndorsingRewardOps.Count;

            Db.EndorsingRewardOps.RemoveRange(Context.EndorsingRewardOps);
            Cache.AppState.ReleaseOperationId(Context.EndorsingRewardOps.Count);
        }
    }
}
