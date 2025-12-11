namespace Tzkt.Sync.Protocols.Proto18
{
    public class Helpers(ProtocolHandler proto) : Proto13.Helpers(proto)
    {
        public override long BakingPower(Data.Models.Delegate baker)
        {
            if (!baker.Staked)
                return 0;

            if (baker.OwnStakedBalance < Context.Protocol.MinimalFrozenStake)
                return 0;

            var limitOfStakingOverBaking = Math.Min(baker.LimitOfStakingOverBaking ?? 0, Context.Protocol.MaxExternalOverOwnStakeRatio * 1_000_000);
            var externalStakeCap = baker.OwnStakedBalance * limitOfStakingOverBaking / 1_000_000;
            var overstaked = Math.Max(0, baker.ExternalStakedBalance - externalStakeCap);
            var totalDelegated = baker.OwnDelegatedBalance + baker.ExternalDelegatedBalance + overstaked;
            var delegationCap = baker.OwnStakedBalance * Context.Protocol.MaxDelegatedOverFrozenRatio;

            var actualStaked = baker.OwnStakedBalance + baker.ExternalStakedBalance - overstaked;
            var actualDelegated = Math.Min(totalDelegated, delegationCap);

            var state = Cache.AppState.Get();
            if (state.AiActivationLevel is int aiLevel && state.Level >= aiLevel)
                actualDelegated /= Context.Protocol.StakePowerMultiplier;

            var stake = actualStaked + actualDelegated;
            if (stake < Context.Protocol.MinimalStake)
                return 0;

            return stake;
        }
        public override long VotingPower(Data.Models.Delegate baker)
        {
            if (!baker.Staked)
                return 0;

            var stake = baker.OwnStakedBalance + baker.ExternalStakedBalance + baker.OwnDelegatedBalance + baker.ExternalDelegatedBalance;
            if (stake < Context.Protocol.MinimalStake)
                return 0;

            return stake;
        }
    }
}
