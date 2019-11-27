using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tzkt.Sync.Protocols.Proto1
{
    class RawTransactionContent : IOperationContent
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

        [JsonPropertyName("amount")]
        public long Amount { get; set; }

        [JsonPropertyName("destination")]
        public string Destination { get; set; }

        [JsonPropertyName("metadata")]
        public RawTransactionContentMetadata Metadata { get; set; }

        #region validation
        public bool IsValidFormat() =>
            !string.IsNullOrEmpty(Source) &&
            Fee >= 0 &&
            Counter >= 0 &&
            GasLimit >= 0 &&
            StorageLimit >= 0 &&
            Amount >= 0 &&
            !string.IsNullOrEmpty(Destination) &&
            Metadata?.IsValidFormat() == true;
        #endregion
    }

    class RawTransactionContentMetadata
    {
        [JsonPropertyName("balance_updates")]
        public List<IBalanceUpdate> BalanceUpdates { get; set; }

        [JsonPropertyName("operation_result")]
        public RawTransactionContentResult Result { get; set; }

        [JsonPropertyName("internal_operation_results")]
        public List<IInternalOperationResult> InternalResults { get; set; }

        #region validation
        public bool IsValidFormat() =>
            BalanceUpdates != null &&
            BalanceUpdates.All(x => x.IsValidFormat()) &&
            Result?.IsValidFormat() == true &&
            (InternalResults == null || InternalResults.All(x => x.IsValidFormat()));
        #endregion
    }

    class RawTransactionContentResult
    {
        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("balance_updates")]
        public List<IBalanceUpdate> BalanceUpdates { get; set; }

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
            ConsumedGas >= 0 &&
            PaidStorageSizeDiff >= 0 &&
            (Errors.ValueKind == JsonValueKind.Array ||
            Errors.ValueKind == JsonValueKind.Undefined);
        #endregion
    }

    class RawInternalTransactionResult : IInternalOperationResult
    {
        [JsonPropertyName("source")]
        public string Source { get; set; }

        [JsonPropertyName("nonce")]
        public int Nonce { get; set; }

        [JsonPropertyName("amount")]
        public long Amount { get; set; }

        [JsonPropertyName("destination")]
        public string Destination { get; set; }

        [JsonPropertyName("result")]
        public RawTransactionContentResult Result { get; set; }

        #region validation
        public bool IsValidFormat() =>
            !string.IsNullOrEmpty(Source) &&
            Nonce >= 0 &&
            Amount >= 0 &&
            !string.IsNullOrEmpty(Destination) &&
            Result?.IsValidFormat() == true;
        #endregion
    }
}
