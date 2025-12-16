using System.Text.Json;

namespace Tzkt.Sync.Protocols.Proto21
{
    class Diagnostics(ProtocolHandler handler) : Proto18.Diagnostics(handler)
    {
        protected override bool CheckMinDelegatedBalance(JsonElement remote, Data.Models.Delegate delegat)
        {
            var minDelegated = remote.Required("min_delegated_in_current_cycle");
            return minDelegated.RequiredInt64("amount") == delegat.MinTotalDelegated &&
                minDelegated.Required("level").RequiredInt32("level") == delegat.MinTotalDelegatedLevel;
        }

        protected override bool CheckFullBalance(JsonElement remote, Data.Models.Delegate delegat)
        {
            return remote.RequiredInt64("own_full_balance") == delegat.Balance;
        }

        protected override bool CheckStakingBalance(JsonElement remote, Data.Models.Delegate delegat)
        {
            return remote.RequiredInt64("total_staked") == delegat.TotalStaked && remote.RequiredInt64("total_delegated") == delegat.TotalDelegated;
        }

        protected override void TestDelegatorsCount(JsonElement remote, Data.Models.Delegate local)
        {
            var delegators = remote.RequiredArray("delegators").Count();
            if (delegators != local.DelegatorsCount && delegators != local.DelegatorsCount + 1)
                throw new Exception($"Diagnostics failed: wrong delegators count {local.Address}");
        }

        protected override bool CheckFrozenDepositLimit(JsonElement remote, Data.Models.Delegate delegat)
        {
            return true;
        }

        protected override bool CheckDelegatedBalance(JsonElement remote, Data.Models.Delegate delegat)
        {
            return remote.RequiredInt64("external_delegated") == delegat.ExternalDelegatedBalance;
        }

        protected override bool CheckBakingPower(JsonElement remote, Data.Models.Delegate delegat)
        {
            var externalStakeCap = 0L;
            if (delegat.LimitOfStakingOverBaking is long limit)
            {
                var (q, r) = Math.DivRem(limit, 1_000_000);
                if (r == 0)
                {
                    externalStakeCap = delegat.OwnStakedBalance * Math.Min(q, Context.Protocol.MaxExternalOverOwnStakeRatio);
                }
                else
                {
                    var limitOfStakingOverBaking = Math.Min(limit, Context.Protocol.MaxExternalOverOwnStakeRatio * 1_000_000);
                    externalStakeCap = delegat.OwnStakedBalance.MulRatio(limitOfStakingOverBaking, 1_000_000);
                }
            }
            var overstaked = Math.Max(0, delegat.ExternalStakedBalance - externalStakeCap);
            var totalDelegated = remote.Required("min_delegated_in_current_cycle").RequiredInt64("amount") + overstaked;
            var delegationCap = delegat.OwnStakedBalance * Context.Protocol.MaxDelegatedOverFrozenRatio;

            var actualStaked = delegat.OwnStakedBalance + delegat.ExternalStakedBalance - overstaked;
            var actualDelegated = Math.Min(totalDelegated, delegationCap);

            var state = Cache.AppState.Get();
            if (state.AiActivationLevel is int aiLevel && state.Level >= aiLevel)
                actualDelegated /= Context.Protocol.StakePowerMultiplier;

            var uncheckedBakingPower = actualStaked + actualDelegated;
            return uncheckedBakingPower == remote.RequiredInt64("baking_power");
        }

        protected override bool CheckVotingPower(JsonElement remote, Data.Models.Delegate delegat)
        {
            var uncheckedVotingPower = delegat.TotalDelegated + delegat.TotalStaked;
            return uncheckedVotingPower == remote.RequiredInt64("current_voting_power");
        }
    }
}
