using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Tzkt.Sync.Protocols.Proto5
{
    class RawRevealContent : IOperationContent
    {
        [JsonProperty("source")]
        [JsonPropertyName("source")]
        public string Source { get; set; }

        [JsonProperty("fee")]
        [JsonPropertyName("fee")]
        public long Fee { get; set; }

        [JsonProperty("counter")]
        [JsonPropertyName("counter")]
        public int Counter { get; set; }

        [JsonProperty("gas_limit")]
        [JsonPropertyName("gas_limit")]
        public int GasLimit { get; set; }

        [JsonProperty("storage_limit")]
        [JsonPropertyName("storage_limit")]
        public int StorageLimit { get; set; }

        [JsonProperty("public_key")]
        [JsonPropertyName("public_key")]
        public string PublicKey { get; set; }

        [JsonProperty("metadata")]
        [JsonPropertyName("metadata")]
        public RawRevealContentMetadata Metadata { get; set; }

        #region validation
        public bool IsValidFormat() =>
            !string.IsNullOrEmpty(Source) &&
            Fee >= 0 &&
            Counter >= 0 &&
            GasLimit >= 0 &&
            StorageLimit >= 0 &&
            !string.IsNullOrEmpty(PublicKey) &&
            Metadata?.IsValidFormat() == true;
        #endregion
    }

    class RawRevealContentMetadata
    {
        [JsonProperty("balance_updates")]
        [JsonPropertyName("balance_updates")]
        public List<IBalanceUpdate> BalanceUpdates { get; set; }

        [JsonProperty("operation_result")]
        [JsonPropertyName("operation_result")]
        public RawRevealContentResult Result { get; set; }

        #region validation
        public bool IsValidFormat() =>
            BalanceUpdates != null &&
            BalanceUpdates.All(x => x.IsValidFormat()) &&
            Result?.IsValidFormat() == true;
        #endregion
    }

    class RawRevealContentResult
    {
        [JsonProperty("status")]
        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonProperty("consumed_gas")]
        [JsonPropertyName("consumed_gas")]
        public int ConsumedGas { get; set; }

        [JsonProperty("errors")]
        [JsonPropertyName("errors")]
        public JsonElement Errors { get; set; }

        #region validation
        public bool IsValidFormat() =>
            !string.IsNullOrEmpty(Status) &&
            (Errors.ValueKind == JsonValueKind.Array ||
            Errors.ValueKind == JsonValueKind.Undefined);
        #endregion
    }
}
