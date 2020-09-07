using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Tzkt.Sync.Protocols.Proto7
{
    interface IBalanceUpdate
    {
        public long Change { get; }
        public string Target { get; }
        bool IsValidFormat();
    }

    class ContractUpdate : IBalanceUpdate
    {
        [JsonProperty("contract")]
        [JsonPropertyName("contract")]
        public string Contract { get; set; }

        [JsonProperty("change")]
        [JsonPropertyName("change")]
        public long Change { get; set; }

        public string Target => Contract;

        #region validation
        public bool IsValidFormat() =>
            !string.IsNullOrEmpty(Contract) &&
            Change != 0;
        #endregion
    }

    class FreezerUpdate : IBalanceUpdate
    {
        [JsonProperty("delegate")]
        [JsonPropertyName("delegate")]
        public string Delegate { get; set; }

        [JsonProperty("cycle")]
        [JsonPropertyName("cycle")]
        public int Cycle { get; set; }

        [JsonProperty("change")]
        [JsonPropertyName("change")]
        public long Change { get; set; }

        public string Target => Delegate;

        #region validation
        public bool IsValidFormat() =>
            !string.IsNullOrEmpty(Delegate) &&
            Cycle >= 0 &&
            Change != 0;
        #endregion
    }

    class DepositsUpdate : FreezerUpdate { }
    class RewardsUpdate : FreezerUpdate { }
    class FeesUpdate : FreezerUpdate { }
}
