using System.Text.Json;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto12
{
    class AttestationRewardCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
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
            var ops = new List<AttestationRewardOperation>(bakerCycles.Count);

            foreach (var (bakerId, bakerCycle) in bakerCycles.Where(x => x.Value.FutureAttestationRewards > 0))
            {
                ops.Add(new()
                {
                    Id = Cache.AppState.NextOperationId(),
                    BakerId = bakerId,
                    Level = block.Level,
                    Timestamp = block.Timestamp,
                    Expected = bakerCycle.FutureAttestationRewards,
                    RewardDelegated = bakerCycle.FutureAttestationRewards
                });

                Db.TryAttach(bakerCycle);
                if (losses.TryGetValue(bakerId, out var loss))
                {
                    if (bakerCycle.FutureAttestationRewards != loss)
                        throw new Exception("FutureAttestationRewards != loss");

                    ops[^1].RewardDelegated = 0; 
                    bakerCycle.MissedAttestationRewards += bakerCycle.FutureAttestationRewards;
                    bakerCycle.FutureAttestationRewards = 0;
                }
                else
                {
                    bakerCycle.AttestationRewardsDelegated += bakerCycle.FutureAttestationRewards;
                    bakerCycle.FutureAttestationRewards = 0;
                }
            }

            foreach (var op in ops)
            {
                var baker = Cache.Accounts.GetDelegate(op.BakerId);
                Db.TryAttach(baker);

                baker.Balance += op.RewardDelegated;
                baker.StakingBalance += op.RewardDelegated;
                baker.AttestationRewardsCount++;

                block.Operations |= Operations.AttestationRewards;

                Cache.Statistics.Current.TotalCreated += op.RewardDelegated;
            }

            Cache.AppState.Get().AttestationRewardOpsCount += ops.Count;

            Db.AttestationRewardOps.AddRange(ops);
            Context.AttestationRewardOps.AddRange(ops);
        }

        public virtual async Task Revert(Block block)
        {
            if (Context.AttestationRewardOps.Count == 0)
                return;

            foreach (var op in Context.AttestationRewardOps)
            {
                var baker = Cache.Accounts.GetDelegate(op.BakerId);
                Db.TryAttach(baker);

                baker.Balance -= op.RewardDelegated;
                baker.StakingBalance -= op.RewardDelegated;
                baker.AttestationRewardsCount--;

                var bakerCycle = await Cache.BakerCycles.GetAsync(block.Cycle, baker.Id);
                Db.TryAttach(bakerCycle);

                bakerCycle.FutureAttestationRewards = op.Expected;
                if (op.Expected == op.RewardDelegated)
                    bakerCycle.AttestationRewardsDelegated -= op.Expected;
                else
                    bakerCycle.MissedAttestationRewards -= op.Expected;
            }

            Cache.AppState.Get().AttestationRewardOpsCount -= Context.AttestationRewardOps.Count;

            Db.AttestationRewardOps.RemoveRange(Context.AttestationRewardOps);
            Cache.AppState.ReleaseOperationId(Context.AttestationRewardOps.Count);
        }
    }
}
