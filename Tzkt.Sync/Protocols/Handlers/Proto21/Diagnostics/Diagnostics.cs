using System.Text.Json;

namespace Tzkt.Sync.Protocols.Proto21
{
    class Diagnostics : Proto18.Diagnostics
    {
        public Diagnostics(ProtocolHandler handler) : base(handler) { }

        protected override bool CheckFullBalance(JsonElement remote, Data.Models.Delegate delegat)
        {
            return remote.RequiredInt64("own_full_balance") == delegat.Balance;
        }

        protected override bool CheckStakingBalance(JsonElement remote, Data.Models.Delegate delegat)
        {
            return remote.RequiredInt64("total_staked") + remote.RequiredInt64("total_delegated") == delegat.StakingBalance;
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
            return remote.RequiredInt64("external_delegated") == delegat.DelegatedBalance;
        }
    }
}
