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

        protected override bool CheckVotingPower(JsonElement remote, Data.Models.Delegate delegat)
        {
            return delegat.VotingPower == remote.RequiredInt64("current_voting_power");
        }
    }
}
