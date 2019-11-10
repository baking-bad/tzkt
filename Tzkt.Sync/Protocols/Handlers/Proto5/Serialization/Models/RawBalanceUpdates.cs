using System.Text.Json.Serialization;

namespace Tzkt.Sync.Protocols.Proto5
{
    interface IBalanceUpdate
    {
        public long Change { get; }
        public string Target { get; }
        bool IsValidFormat();
    }

    class ContractUpdate : IBalanceUpdate
    {
        [JsonPropertyName("contract")]
        public string Contract { get; set; }

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
        [JsonPropertyName("delegate")]
        public string Delegate { get; set; }

        [JsonPropertyName("cycle")]
        public int Cycle { get; set; }

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
