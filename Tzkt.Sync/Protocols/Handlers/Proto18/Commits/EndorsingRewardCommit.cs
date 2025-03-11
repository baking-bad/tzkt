using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto18
{
    class EndorsingRewardCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public virtual async Task Apply(Block block, JsonElement rawBlock)
        {
            if (!block.Events.HasFlag(BlockEvents.CycleEnd))
                return;

            var state = Cache.AppState.Get();
            var bakerCycles = await Cache.BakerCycles.GetAsync(block.Cycle);
            var ops = bakerCycles
                .Where(x => x.Value.FutureEndorsementRewards > 0)
                .ToDictionary(
                    x => x.Key,
                    bakerCycle => new EndorsingRewardOperation
                    {
                        Id = Cache.AppState.NextOperationId(),
                        BakerId = bakerCycle.Key,
                        Level = block.Level,
                        Timestamp = block.Timestamp,
                        Expected = bakerCycle.Value.FutureEndorsementRewards
                    });

            var balanceUpdates = rawBlock.Required("metadata").RequiredArray("balance_updates").EnumerateArray().Where(x => x.RequiredString("origin") == "block").ToList();
            for (int i = 0; i < balanceUpdates.Count; i++)
            {
                var update = balanceUpdates[i];
                if (update.RequiredString("kind") == "minted" && update.RequiredString("category") == "endorsing rewards")
                {
                    if (i == balanceUpdates.Count - 1)
                        throw new Exception("Unexpected endorsing rewards balance updates behavior");

                    var change = -update.RequiredInt64("change");
                        
                    var nextUpdate = balanceUpdates[i + 1];
                    if (nextUpdate.RequiredString("kind") == "freezer" &&
                        nextUpdate.RequiredString("category") == "deposits" &&
                        nextUpdate.RequiredInt64("change") == change)
                    {
                        var baker = Cache.Accounts.GetExistingDelegate(nextUpdate.Required("staker").RequiredString("baker"));
                        if (!ops.TryGetValue(baker.Id, out var op))
                            throw new Exception("Unexpected endorsing rewards balance update");

                        if (baker.ExternalStakedBalance != 0)
                            throw new Exception("Manual staking should be disabled in Oxford");

                        op.RewardStakedOwn = change;
                    }
                    else if (nextUpdate.RequiredString("kind") == "contract" &&
                        nextUpdate.RequiredInt64("change") == change)
                    {
                        var baker = Cache.Accounts.GetExistingDelegate(nextUpdate.RequiredString("contract"));
                        if (!ops.TryGetValue(baker.Id, out var op))
                            throw new Exception("Unexpected endorsing rewards balance update");

                        op.RewardDelegated = change;
                    }
                    else if (nextUpdate.RequiredString("kind") == "burned" &&
                        nextUpdate.RequiredString("category") == "lost endorsing rewards" &&
                        nextUpdate.RequiredInt64("change") == change)
                    {
                        var baker = Cache.Accounts.GetExistingDelegate(nextUpdate.RequiredString("delegate"));
                        if (!ops.TryGetValue(baker.Id, out var op))
                            throw new Exception("Unexpected endorsing rewards balance update");

                        if (op.Expected != change)
                            throw new Exception("FutureEndorsementRewards != loss");

                        op.RewardDelegated = 0;
                        op.RewardStakedOwn = 0;
                        op.RewardStakedEdge = 0;
                        op.RewardStakedShared = 0;
                    }
                    else
                    {
                        throw new Exception("Unexpected endorsing rewards balance updates behavior");
                    }
                }
            }

            foreach (var op in ops.Values)
            {
                var bakerCycle = bakerCycles[op.BakerId];
                Db.TryAttach(bakerCycle);

                bakerCycle.FutureEndorsementRewards = 0;
                if (op.RewardDelegated != 0 || op.RewardStakedOwn != 0 || op.RewardStakedEdge != 0 || op.RewardStakedShared != 0)
                {
                    if (op.Expected != op.RewardDelegated + op.RewardStakedOwn + op.RewardStakedEdge + op.RewardStakedShared)
                        throw new Exception("ExpectedReward != RewardFrozen + RewardDelegated");

                    bakerCycle.EndorsementRewardsDelegated = op.RewardDelegated;
                    bakerCycle.EndorsementRewardsStakedOwn = op.RewardStakedOwn;
                    bakerCycle.EndorsementRewardsStakedEdge = op.RewardStakedEdge;
                    bakerCycle.EndorsementRewardsStakedShared = op.RewardStakedShared;
                }
                else
                {
                    bakerCycle.MissedEndorsementRewards = op.Expected;
                }


                var baker = Cache.Accounts.GetDelegate(op.BakerId);
                Db.TryAttach(baker);

                baker.Balance += op.RewardDelegated + op.RewardStakedOwn + op.RewardStakedEdge;
                baker.StakingBalance += op.RewardDelegated + op.RewardStakedOwn + op.RewardStakedEdge + op.RewardStakedShared;
                baker.OwnStakedBalance += op.RewardStakedOwn + op.RewardStakedEdge;
                baker.ExternalStakedBalance += op.RewardStakedShared;
                baker.EndorsingRewardsCount++;

                block.Operations |= Operations.EndorsingRewards;

                Cache.Statistics.Current.TotalCreated += op.RewardDelegated + op.RewardStakedOwn + op.RewardStakedEdge + op.RewardStakedShared;
                Cache.Statistics.Current.TotalFrozen += op.RewardStakedOwn + op.RewardStakedEdge + op.RewardStakedShared;
            }

            Cache.AppState.Get().EndorsingRewardOpsCount += ops.Count;

            Db.EndorsingRewardOps.AddRange(ops.Values);
            Context.EndorsingRewardOps.AddRange(ops.Values);
        }

        public virtual async Task Revert(Block block)
        {
            if (!block.Operations.HasFlag(Operations.EndorsingRewards))
                return;

            var ops = await Db.EndorsingRewardOps.Where(x => x.Level == block.Level).ToListAsync();

            foreach (var op in ops)
            {
                var bakerCycle = await Cache.BakerCycles.GetAsync(block.Cycle, op.BakerId);
                Db.TryAttach(bakerCycle);

                bakerCycle.FutureEndorsementRewards = op.Expected;
                bakerCycle.MissedEndorsementRewards = 0;
                bakerCycle.EndorsementRewardsDelegated = 0;
                bakerCycle.EndorsementRewardsStakedOwn = 0;
                bakerCycle.EndorsementRewardsStakedEdge = 0;
                bakerCycle.EndorsementRewardsStakedShared = 0;

                var baker = Cache.Accounts.GetDelegate(op.BakerId);
                Db.TryAttach(baker);

                baker.Balance -= op.RewardDelegated + op.RewardStakedOwn + op.RewardStakedEdge;
                baker.StakingBalance -= op.RewardDelegated + op.RewardStakedOwn + op.RewardStakedEdge + op.RewardStakedShared;
                baker.OwnStakedBalance -= op.RewardStakedOwn + op.RewardStakedEdge;
                baker.ExternalStakedBalance -= op.RewardStakedShared;
                baker.EndorsingRewardsCount--;
            }

            Cache.AppState.Get().EndorsingRewardOpsCount -= ops.Count;

            Db.EndorsingRewardOps.RemoveRange(ops);
            Cache.AppState.ReleaseOperationId(ops.Count);
        }
    }
}
