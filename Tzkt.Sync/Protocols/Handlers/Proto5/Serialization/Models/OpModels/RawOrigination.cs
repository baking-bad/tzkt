using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tzkt.Sync.Protocols.Proto5
{
    class RawOriginationContent : IOperationContent
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

        [JsonPropertyName("balance")]
        public long Balance { get; set; }

        [JsonPropertyName("delegate")]
        public string Delegate { get; set; }

        [JsonPropertyName("script")]
        public RawOriginationScript Script { get; set; }

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
        [JsonPropertyName("code")]
        public object Code { get; set; }

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
        [JsonPropertyName("balance_updates")]
        public List<IBalanceUpdate> BalanceUpdates { get; set; }

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
        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("balance_updates")]
        public List<IBalanceUpdate> BalanceUpdates { get; set; }

        [JsonPropertyName("originated_contracts")]
        public List<string> OriginatedContracts { get; set; }

        [JsonPropertyName("consumed_gas")]
        public int ConsumedGas { get; set; }

        [JsonPropertyName("paid_storage_size_diff")]
        public int PaidStorageSizeDiff { get; set; }

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
