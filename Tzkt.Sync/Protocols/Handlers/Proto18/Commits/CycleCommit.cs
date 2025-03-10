using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto18
{
    class CycleCommit : Proto14.CycleCommit
    {
        public CycleCommit(ProtocolHandler protocol) : base(protocol) { }

        public override async Task Apply(Block block)
        {
            if (!block.Events.HasFlag(BlockEvents.CycleBegin))
                return;

            await base.Apply(block);

            var res = await Proto.Rpc.GetExpectedIssuance(block.Level);
            var issuance = res.EnumerateArray().First(x => x.RequiredInt32("cycle") == FutureCycle.Index);

            FutureCycle.BlockReward = issuance.RequiredInt64("baking_reward_fixed_portion");
            FutureCycle.BlockBonusPerSlot = issuance.RequiredInt64("baking_reward_bonus_per_slot");
            FutureCycle.MaxBlockReward = FutureCycle.BlockReward + FutureCycle.BlockBonusPerSlot * (Context.Protocol.EndorsersPerBlock - Context.Protocol.ConsensusThreshold);
            FutureCycle.EndorsementRewardPerSlot = issuance.RequiredInt64("attesting_reward_per_slot");
            FutureCycle.NonceRevelationReward = issuance.RequiredInt64("seed_nonce_revelation_tip");
            FutureCycle.VdfRevelationReward = issuance.RequiredInt64("vdf_revelation_tip");
        }

        protected override async Task<Dictionary<int, long>> GetSelectedStakes(Block block, Protocol protocol)
        {
            if (block.Cycle == protocol.FirstCycle)
                return await base.GetSelectedStakes(block, protocol);

            var slashings = new Dictionary<int, int>();
            var prevBlock = Cache.Blocks.Get(block.Level - 1);
            if (prevBlock.Events.HasFlag(BlockEvents.DoubleBakingSlashing))
            {
                var prevBlockProto = await Cache.Protocols.GetAsync(prevBlock.ProtoCode);
                foreach (var op in await Db.DoubleBakingOps.AsNoTracking().Where(x => x.SlashedLevel == block.Level - 1).ToListAsync())
                    slashings[op.OffenderId] = slashings.GetValueOrDefault(op.OffenderId) + prevBlockProto.DoubleBakingSlashedPercentage;
            }
            if (prevBlock.Events.HasFlag(BlockEvents.DoubleEndorsingSlashing))
            {
                var prevBlockProto = await Cache.Protocols.GetAsync(prevBlock.ProtoCode);
                foreach (var op in await Db.DoubleEndorsingOps.AsNoTracking().Where(x => x.SlashedLevel == block.Level - 1).ToListAsync())
                    slashings[op.OffenderId] = slashings.GetValueOrDefault(op.OffenderId) + prevBlockProto.DoubleEndorsingSlashedPercentage;
            }
            if (prevBlock.Events.HasFlag(BlockEvents.DoublePreendorsingSlashing))
            {
                var prevBlockProto = await Cache.Protocols.GetAsync(prevBlock.ProtoCode);
                foreach (var op in await Db.DoublePreendorsingOps.AsNoTracking().Where(x => x.SlashedLevel == block.Level - 1).ToListAsync())
                    slashings[op.OffenderId] = slashings.GetValueOrDefault(op.OffenderId) + prevBlockProto.DoubleEndorsingSlashedPercentage;
            }

            return Snapshots.Select(x =>
            {
                var ownStaked = x.OwnStakedBalance;
                var externalStaked = x.ExternalStakedBalance;
                if (slashings.TryGetValue(x.AccountId, out var percentage))
                {
                    ownStaked = ownStaked * Math.Max(0, 10_000 - percentage) / 10_000;
                    externalStaked = externalStaked * Math.Max(0, 10_000 - percentage) / 10_000;
                }
                var totalStaked = ownStaked + externalStaked;

                var stakingOverBaking = Math.Min(
                    protocol.MaxExternalOverOwnStakeRatio * 1_000_000,
                    Cache.Accounts.GetDelegate(x.AccountId).LimitOfStakingOverBaking ?? long.MaxValue);

                var frozen = Math.Min(totalStaked, ownStaked + (long)((BigInteger)ownStaked * stakingOverBaking / 1_000_000));
                var delegated = Math.Min(x.StakingBalance - frozen, ownStaked * protocol.MaxDelegatedOverFrozenRatio);

                return (x.AccountId, frozen, delegated);
            })
            .Where(x => x.frozen >= protocol.MinimalFrozenStake && x.frozen + x.delegated >= protocol.MinimalStake)
            .ToDictionary(x => x.AccountId, x =>
            {
                return x.frozen + x.delegated;
            });
        }
    }
}
