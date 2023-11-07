using System.Numerics;
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
            FutureCycle.MaxBlockReward = FutureCycle.BlockReward + FutureCycle.BlockBonusPerSlot * (block.Protocol.EndorsersPerBlock - block.Protocol.ConsensusThreshold);
            FutureCycle.EndorsementRewardPerSlot = issuance.RequiredInt64("attesting_reward_per_slot");
            FutureCycle.NonceRevelationReward = issuance.RequiredInt64("seed_nonce_revelation_tip");
            FutureCycle.VdfRevelationReward = issuance.RequiredInt64("vdf_revelation_tip");
            FutureCycle.LBSubsidy = issuance.RequiredInt64("liquidity_baking_subsidy");
        }

        protected override Task<Dictionary<int, long>> GetSelectedStakes(Block block)
        {
            if (block.Cycle == block.Protocol.FirstCycle)
                return base.GetSelectedStakes(block);

            if (block.Cycle <= Cache.AppState.Get().AIActivationCycle)
                return Task.FromResult(Snapshots
                    .Where(x => x.StakingBalance >= block.Protocol.MinimalStake)
                    .ToDictionary(x => x.AccountId, x => Math.Min(x.StakingBalance, x.TotalStakedBalance * (block.Protocol.MaxDelegatedOverFrozenRatio + 1))));

            return Task.FromResult(Snapshots
                .Select(x =>
                {
                    var stakingOverBaking = Math.Min(
                        block.Protocol.MaxExternalOverOwnStakeRatio * 1_000_000,
                        Cache.Accounts.GetDelegate(x.AccountId).LimitOfStakingOverBaking ?? long.MaxValue);

                    var frozen = Math.Min(x.TotalStakedBalance, x.OwnStakedBalance + (long)((BigInteger)x.OwnStakedBalance* stakingOverBaking / 1_000_000));
                    var delegated = Math.Min(x.StakingBalance - frozen, x.OwnStakedBalance * block.Protocol.MaxDelegatedOverFrozenRatio);

                    return (x.AccountId, frozen, delegated);
                })
                .Where(x => x.frozen >= block.Protocol.MinimalFrozenStake && x.frozen + x.delegated >= block.Protocol.MinimalStake)
                .ToDictionary(x => x.AccountId, x =>
                {
                    return x.frozen + x.delegated / block.Protocol.StakePowerMultiplier;
                }));
        }
    }
}
