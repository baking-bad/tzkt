using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Tzkt.Sync.Protocols.Proto5
{
    class RawOriginationContent : IOperationContent
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

        [JsonProperty("balance")]
        [JsonPropertyName("balance")]
        public long Balance { get; set; }

        [JsonProperty("delegate")]
        [JsonPropertyName("delegate")]
        public string Delegate { get; set; }

        [JsonProperty("script")]
        [JsonPropertyName("script")]
        public RawOriginationScript Script { get; set; }

        [JsonProperty("metadata")]
        [JsonPropertyName("metadata")]
        public RawOriginationContentMetadata Metadata { get; set; }

        #region validation
        public bool IsValidFormat() =>
            !string.IsNullOrEmpty(Source) &&
            Fee >= 0 &&
            Counter >= 0 &&
            GasLimit >= 0 &&
            StorageLimit >= 0 &&
            Balance >= 0 &&
            (Delegate == null || Delegate != "") &&
            (Script == null || Script.IsValidFormat()) &&
            Metadata?.IsValidFormat() == true;
        #endregion
    }

    class RawOriginationScript
    {
        [JsonProperty("code")]
        [JsonPropertyName("code")]
        public object Code { get; set; }

        [JsonProperty("storage")]
        [JsonPropertyName("storage")]
        public object Storage { get; set; }

        #region validation
        public bool IsValidFormat() =>
            Code != null &&
            Storage != null;
        #endregion
    }

    class RawOriginationContentMetadata
    {
        [JsonProperty("balance_updates")]
        [JsonPropertyName("balance_updates")]
        public List<IBalanceUpdate> BalanceUpdates { get; set; }

        [JsonProperty("operation_result")]
        [JsonPropertyName("operation_result")]
        public RawOriginationContentResult Result { get; set; }

        #region validation
        public bool IsValidFormat() =>
            BalanceUpdates != null &&
            BalanceUpdates.All(x => x.IsValidFormat()) &&
            Result?.IsValidFormat() == true;
        #endregion
    }

    class RawOriginationContentResult
    {
        [JsonProperty("status")]
        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonProperty("balance_updates")]
        [JsonPropertyName("balance_updates")]
        public List<IBalanceUpdate> BalanceUpdates { get; set; }

        [JsonProperty("originated_contracts")]
        [JsonPropertyName("originated_contracts")]
        public List<string> OriginatedContracts { get; set; }

        [JsonProperty("consumed_gas")]
        [JsonPropertyName("consumed_gas")]
        public int ConsumedGas { get; set; }

        [JsonProperty("paid_storage_size_diff")]
        [JsonPropertyName("paid_storage_size_diff")]
        public int PaidStorageSizeDiff { get; set; }

        [JsonProperty("errors")]
        [JsonPropertyName("errors")]
        public JsonElement Errors { get; set; }

        #region validation
        public bool IsValidFormat() =>
            !string.IsNullOrEmpty(Status) &&
            (BalanceUpdates == null || BalanceUpdates.All(x => x.IsValidFormat())) &&
            (OriginatedContracts == null || OriginatedContracts?.Count == 1) &&
            ConsumedGas >= 0 &&
            PaidStorageSizeDiff >= 0 &&
            (Errors.ValueKind == JsonValueKind.Array ||
            Errors.ValueKind == JsonValueKind.Undefined);
        #endregion
    }
}
