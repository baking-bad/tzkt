using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tzkt.Sync.Protocols.Proto5
{
    class RawRevealContent : IOperationContent
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

        [JsonPropertyName("public_key")]
        public string PublicKey { get; set; }

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
        [JsonPropertyName("balance_updates")]
        public List<IBalanceUpdate> BalanceUpdates { get; set; }

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
        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("consumed_gas")]
        public int ConsumedGas { get; set; }

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
