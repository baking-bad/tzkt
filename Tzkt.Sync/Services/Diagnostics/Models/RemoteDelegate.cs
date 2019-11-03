using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Tzkt.Sync.Services.Diagnostics
{
    class RemoteDelegate
    {
        [JsonPropertyName("balance")]
        public long? Balance { get; set; }

        [JsonPropertyName("frozen_balance_by_cycle")]
        public List<RemoteDelegateFreeze> FrozenBalances { get; set; }

        [JsonPropertyName("staking_balance")]
        public long? StakingBalance { get; set; }

        [JsonPropertyName("delegated_contracts")]
        public List<int> Delegators { get; set; }

        [JsonPropertyName("deactivated")]
        public bool? Deactivated { get; set; }

        [JsonPropertyName("grace_period")]
        public int? GracePeriod { get; set; }

        #region validation
        public bool IsValidFormat() =>
            Balance != null &&
            FrozenBalances.All(x => x.IsValidFormat()) &&
            StakingBalance != null &&
            Delegators != null &&
            Deactivated != null &&
            GracePeriod != null;
        #endregion
    }

    class RemoteDelegateFreeze
    {
        [JsonPropertyName("cycle")]
        public int? Cycle { get; set; }

        [JsonPropertyName("deposit")]
        public long? Deposit { get; set; }

        [JsonPropertyName("fees")]
        public long? Fees { get; set; }

        [JsonPropertyName("rewards")]
        public long? Rewards { get; set; }

        #region validation
        public bool IsValidFormat() =>
            Cycle != null &&
            Deposit != null &&
            Fees != null &&
            Rewards != null;
        #endregion
    }
}
