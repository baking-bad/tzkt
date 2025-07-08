using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Netezos.Encoding;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto20
{
    class CycleCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public Cycle? FutureCycle { get; protected set; }
        public List<SnapshotBalance>? Snapshots { get; protected set; }
        public Dictionary<int, long>? SelectedStakes { get; protected set; }

        public virtual async Task Apply(Block block)
        {
            if (!block.Events.HasFlag(BlockEvents.CycleBegin))
                return;

            var index = block.Cycle + Context.Protocol.ConsensusRightsDelay;

            var contextTask = Proto.Rpc.GetCycleAsync(block.Level, index);
            var issuanceTask = Proto.Rpc.GetExpectedIssuance(block.Level);
            await Task.WhenAll(contextTask, issuanceTask);

            var context = await contextTask;
            var issuance = await issuanceTask;
            var cycleIssuance = issuance.EnumerateArray().First(x => x.RequiredInt32("cycle") == index);

            SelectedStakes = context.RequiredArray("selected_stake_distribution")
                .EnumerateArray()
                .ToDictionary(
                    x => Cache.Accounts.GetExistingDelegate(x.RequiredString("baker")).Id,
                    x => x.Required("active_stake").RequiredInt64("frozen") + x.Required("active_stake").RequiredInt64("delegated"));

            Snapshots = await Db.SnapshotBalances
                .AsNoTracking()
                .Where(x => x.Level == block.Level - 1 && x.BakerId == x.AccountId)
                .ToListAsync();

            FutureCycle = new Cycle
            {
                Id = 0,
                Index = index,
                FirstLevel = Context.Protocol.GetCycleStart(index),
                LastLevel = Context.Protocol.GetCycleEnd(index),
                SnapshotLevel = block.Level - 1,
                TotalBakers = SelectedStakes.Count,
                TotalBakingPower = SelectedStakes.Values.Sum(),
                Seed = Hex.Parse(context.RequiredString("random_seed")),
                BlockReward = cycleIssuance.RequiredInt64("baking_reward_fixed_portion"),
                BlockBonusPerSlot = cycleIssuance.RequiredInt64("baking_reward_bonus_per_slot"),
                AttestationRewardPerSlot = cycleIssuance.RequiredInt64("attesting_reward_per_slot"),
                NonceRevelationReward = cycleIssuance.RequiredInt64("seed_nonce_revelation_tip"),
                VdfRevelationReward = cycleIssuance.RequiredInt64("vdf_revelation_tip"),
                DalAttestationRewardPerShard = GetDalAttestationRewardPerShard(cycleIssuance)
            };

            FutureCycle.MaxBlockReward = FutureCycle.BlockReward
                + FutureCycle.BlockBonusPerSlot * (Context.Protocol.AttestersPerBlock - Context.Protocol.ConsensusThreshold);

            Db.Cycles.Add(FutureCycle);
        }

        public virtual async Task Revert(Block block)
        {
            if (!block.Events.HasFlag(BlockEvents.CycleBegin))
                return;

            await Db.Database.ExecuteSqlRawAsync("""
                DELETE FROM "Cycles"
                WHERE "Index" = {0}
                """, block.Cycle + Context.Protocol.ConsensusRightsDelay);
        }

        protected virtual long GetDalAttestationRewardPerShard(JsonElement issuance) => 0;
    }
}
