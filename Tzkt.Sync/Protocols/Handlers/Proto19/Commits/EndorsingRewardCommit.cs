using System.Text.Json;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto19
{
    class EndorsingRewardCommit : Proto18.EndorsingRewardCommit
    {
        public EndorsingRewardCommit(ProtocolHandler protocol) : base(protocol) { }

        public override async Task Apply(Block block, JsonElement rawBlock)
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
                if (update.RequiredString("kind") == "minted" && update.RequiredString("category") == "attesting rewards")
                {
                    if (i == balanceUpdates.Count - 1)
                        throw new Exception("Unexpected attesting rewards balance updates behavior");

                    var change = -update.RequiredInt64("change");

                    var nextUpdate = balanceUpdates[i + 1];
                    if (nextUpdate.RequiredInt64("change") != change)
                        throw new Exception("Unexpected attesting rewards balance updates behavior");

                    if (nextUpdate.RequiredString("kind") == "freezer" && nextUpdate.RequiredString("category") == "deposits")
                    {
                        var staker = nextUpdate.Required("staker");
                        if (staker.TryGetProperty("baker_own_stake", out var p))
                        {
                            var baker = Cache.Accounts.GetDelegate(p.GetString());
                            if (!ops.TryGetValue(baker.Id, out var op))
                                throw new Exception("Unexpected attesting rewards balance update");

                            op.RewardStakedOwn += change;
                        }
                        else if (staker.TryGetProperty("baker_edge", out p))
                        {
                            var baker = Cache.Accounts.GetDelegate(p.GetString());
                            if (!ops.TryGetValue(baker.Id, out var op))
                                throw new Exception("Unexpected attesting rewards balance update");

                            op.RewardStakedEdge += change;
                        }
                        else if (staker.TryGetProperty("delegate", out p))
                        {
                            var baker = Cache.Accounts.GetDelegate(p.GetString());
                            if (!ops.TryGetValue(baker.Id, out var op))
                                throw new Exception("Unexpected attesting rewards balance update");

                            op.RewardStakedShared += change;
                        }
                        else
                        {
                            throw new Exception("Unexpected attesting rewards balance updates behavior");
                        }
                    }
                    else if (nextUpdate.RequiredString("kind") == "contract")
                    {
                        var baker = Cache.Accounts.GetDelegate(nextUpdate.RequiredString("contract"));
                        if (!ops.TryGetValue(baker.Id, out var op))
                            throw new Exception("Unexpected attesting rewards balance update");

                        op.RewardDelegated = change;
                    }
                    else if (nextUpdate.RequiredString("kind") == "burned" && nextUpdate.RequiredString("category") == "lost attesting rewards")
                    {
                        var baker = Cache.Accounts.GetDelegate(nextUpdate.RequiredString("delegate"));
                        if (!ops.TryGetValue(baker.Id, out var op))
                            throw new Exception("Unexpected attesting rewards balance update");

                        if (op.Expected != change)
                            throw new Exception("FutureEndorsementRewards != loss");

                        op.RewardDelegated = 0;
                        op.RewardStakedOwn = 0;
                        op.RewardStakedEdge = 0;
                        op.RewardStakedShared = 0;
                    }
                    else
                    {
                        throw new Exception("Unexpected attesting rewards balance updates behavior");
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
    }
}
