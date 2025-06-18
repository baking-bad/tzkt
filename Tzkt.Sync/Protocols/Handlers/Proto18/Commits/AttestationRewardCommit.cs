using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto18
{
    class AttestationRewardCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public virtual async Task Apply(Block block, JsonElement rawBlock)
        {
            if (!block.Events.HasFlag(BlockEvents.CycleEnd))
                return;

            var state = Cache.AppState.Get();
            var bakerCycles = await Cache.BakerCycles.GetAsync(block.Cycle);
            var ops = bakerCycles
                .Where(x => x.Value.FutureAttestationRewards > 0)
                .ToDictionary(
                    x => x.Key,
                    bakerCycle => new AttestationRewardOperation
                    {
                        Id = Cache.AppState.NextOperationId(),
                        BakerId = bakerCycle.Key,
                        Level = block.Level,
                        Timestamp = block.Timestamp,
                        Expected = bakerCycle.Value.FutureAttestationRewards
                    });

            var balanceUpdates = rawBlock.Required("metadata").RequiredArray("balance_updates").EnumerateArray().Where(x => x.RequiredString("origin") == "block").ToList();
            for (int i = 0; i < balanceUpdates.Count; i++)
            {
                var update = balanceUpdates[i];
                if (update.RequiredString("kind") == "minted" && (update.RequiredString("category") == "endorsing rewards" || update.RequiredString("category") == "attesting rewards"))
                {
                    if (i == balanceUpdates.Count - 1)
                        throw new Exception("Unexpected attestation rewards balance updates behavior");

                    var change = -update.RequiredInt64("change");
                        
                    var nextUpdate = balanceUpdates[i + 1];
                    if (nextUpdate.RequiredString("kind") == "freezer" &&
                        nextUpdate.RequiredString("category") == "deposits" &&
                        nextUpdate.RequiredInt64("change") == change)
                    {
                        var baker = Cache.Accounts.GetExistingDelegate(nextUpdate.Required("staker").RequiredString("baker"));
                        if (!ops.TryGetValue(baker.Id, out var op))
                            throw new Exception("Unexpected attestation rewards balance update");

                        if (baker.ExternalStakedBalance != 0)
                            throw new Exception("Manual staking should be disabled in Oxford");

                        op.RewardStakedOwn = change;
                    }
                    else if (nextUpdate.RequiredString("kind") == "contract" &&
                        nextUpdate.RequiredInt64("change") == change)
                    {
                        var baker = Cache.Accounts.GetExistingDelegate(nextUpdate.RequiredString("contract"));
                        if (!ops.TryGetValue(baker.Id, out var op))
                            throw new Exception("Unexpected attestation rewards balance update");

                        op.RewardDelegated = change;
                    }
                    else if (nextUpdate.RequiredString("kind") == "burned" &&
                        (nextUpdate.RequiredString("category") == "lost endorsing rewards" || nextUpdate.RequiredString("category") == "lost attesting rewards") &&
                        nextUpdate.RequiredInt64("change") == change)
                    {
                        var baker = Cache.Accounts.GetExistingDelegate(nextUpdate.RequiredString("delegate"));
                        if (!ops.TryGetValue(baker.Id, out var op))
                            throw new Exception("Unexpected attestation rewards balance update");

                        if (op.Expected != change)
                            throw new Exception("FutureAttestationRewards != loss");

                        op.RewardDelegated = 0;
                        op.RewardStakedOwn = 0;
                        op.RewardStakedEdge = 0;
                        op.RewardStakedShared = 0;
                    }
                    else
                    {
                        throw new Exception("Unexpected attestation rewards balance updates behavior");
                    }
                }
            }

            foreach (var op in ops.Values)
            {
                var bakerCycle = bakerCycles[op.BakerId];
                Db.TryAttach(bakerCycle);

                bakerCycle.FutureAttestationRewards = 0;
                if (op.RewardDelegated != 0 || op.RewardStakedOwn != 0 || op.RewardStakedEdge != 0 || op.RewardStakedShared != 0)
                {
                    if (op.Expected != op.RewardDelegated + op.RewardStakedOwn + op.RewardStakedEdge + op.RewardStakedShared)
                        throw new Exception("ExpectedReward != RewardFrozen + RewardDelegated");

                    bakerCycle.AttestationRewardsDelegated = op.RewardDelegated;
                    bakerCycle.AttestationRewardsStakedOwn = op.RewardStakedOwn;
                    bakerCycle.AttestationRewardsStakedEdge = op.RewardStakedEdge;
                    bakerCycle.AttestationRewardsStakedShared = op.RewardStakedShared;
                }
                else
                {
                    bakerCycle.MissedAttestationRewards = op.Expected;
                }


                var baker = Cache.Accounts.GetDelegate(op.BakerId);
                Db.TryAttach(baker);

                baker.Balance += op.RewardDelegated + op.RewardStakedOwn + op.RewardStakedEdge;
                baker.StakingBalance += op.RewardDelegated + op.RewardStakedOwn + op.RewardStakedEdge + op.RewardStakedShared;
                baker.OwnStakedBalance += op.RewardStakedOwn + op.RewardStakedEdge;
                baker.ExternalStakedBalance += op.RewardStakedShared;
                baker.AttestationRewardsCount++;

                block.Operations |= Operations.AttestationRewards;

                Cache.Statistics.Current.TotalCreated += op.RewardDelegated + op.RewardStakedOwn + op.RewardStakedEdge + op.RewardStakedShared;
                Cache.Statistics.Current.TotalFrozen += op.RewardStakedOwn + op.RewardStakedEdge + op.RewardStakedShared;
            }

            Cache.AppState.Get().AttestationRewardOpsCount += ops.Count;

            Db.AttestationRewardOps.AddRange(ops.Values);
            Context.AttestationRewardOps.AddRange(ops.Values);
        }

        public virtual async Task Revert(Block block)
        {
            if (!block.Operations.HasFlag(Operations.AttestationRewards))
                return;

            var ops = await Db.AttestationRewardOps.Where(x => x.Level == block.Level).ToListAsync();

            foreach (var op in ops)
            {
                var bakerCycle = await Cache.BakerCycles.GetAsync(block.Cycle, op.BakerId);
                Db.TryAttach(bakerCycle);

                bakerCycle.FutureAttestationRewards = op.Expected;
                bakerCycle.MissedAttestationRewards = 0;
                bakerCycle.AttestationRewardsDelegated = 0;
                bakerCycle.AttestationRewardsStakedOwn = 0;
                bakerCycle.AttestationRewardsStakedEdge = 0;
                bakerCycle.AttestationRewardsStakedShared = 0;

                var baker = Cache.Accounts.GetDelegate(op.BakerId);
                Db.TryAttach(baker);

                baker.Balance -= op.RewardDelegated + op.RewardStakedOwn + op.RewardStakedEdge;
                baker.StakingBalance -= op.RewardDelegated + op.RewardStakedOwn + op.RewardStakedEdge + op.RewardStakedShared;
                baker.OwnStakedBalance -= op.RewardStakedOwn + op.RewardStakedEdge;
                baker.ExternalStakedBalance -= op.RewardStakedShared;
                baker.AttestationRewardsCount--;
            }

            Cache.AppState.Get().AttestationRewardOpsCount -= ops.Count;

            Db.AttestationRewardOps.RemoveRange(ops);
            Cache.AppState.ReleaseOperationId(ops.Count);
        }
    }
}
