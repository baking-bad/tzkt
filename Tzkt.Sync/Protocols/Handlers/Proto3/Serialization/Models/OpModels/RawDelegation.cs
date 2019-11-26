using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tzkt.Sync.Protocols.Proto3
{
    class RawDelegationContent : IOperationContent
    {
        [JsonPropertyName("source")]
        public string Source { get; set; }

        [JsonPropertyName("fee")]
        public long Fee { get; set; }

        [JsonPropertyName("counter")]
        public int Counter { get; set; }

        [JsonPropertyName("gas_limit")]
        public int GasLimit { get; set; }

        [JsonPropertyName("storage_limit")]
        public int StorageLimit { get; set; }

        [JsonPropertyName("delegate")]
        public string Delegate { get; set; }

        [JsonPropertyName("metadata")]
        public RawDelegationContentMetadata Metadata { get; set; }

        #region validation
        public bool IsValidFormat() =>
            !string.IsNullOrEmpty(Source) &&
            Fee >= 0 &&
            Counter >= 0 &&
            GasLimit >= 0 &&
            StorageLimit >= 0 &&
            (Delegate == null || Delegate != "") &&
            Metadata?.IsValidFormat() == true;
        #endregion
    }

    class RawDelegationContentMetadata
    {
        [JsonPropertyName("balance_updates")]
        public List<IBalanceUpdate> BalanceUpdates { get; set; }

        [JsonPropertyName("operation_result")]
        public RawDelegationContentResult Result { get; set; }

        #region validation
        public bool IsValidFormat() =>
            BalanceUpdates != null &&
            BalanceUpdates.All(x => x.IsValidFormat()) &&
            Result?.IsValidFormat() == true;
        #endregion
    }

    class RawDelegationContentResult
    {
        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("consumed_gas")]
        public int ConsumedGas { get; set; }

        [JsonPropertyName("errors")]
        public JsonElement Errors { get; set; }

        #region validation
        public bool IsValidFormat() =>
            !string.IsNullOrEmpty(Status);
        #endregion
    }
}
