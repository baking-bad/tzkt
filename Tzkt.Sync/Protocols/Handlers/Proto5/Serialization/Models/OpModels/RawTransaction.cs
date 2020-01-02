using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Tzkt.Sync.Protocols.Proto5
{
    class RawTransactionContent : IOperationContent
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

        [JsonProperty("amount")]
        [JsonPropertyName("amount")]
        public long Amount { get; set; }

        [JsonProperty("destination")]
        [JsonPropertyName("destination")]
        public string Destination { get; set; }

        [JsonProperty("metadata")]
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
        [JsonProperty("balance_updates")]
        [JsonPropertyName("balance_updates")]
        public List<IBalanceUpdate> BalanceUpdates { get; set; }

        [JsonProperty("operation_result")]
        [JsonPropertyName("operation_result")]
        public RawTransactionContentResult Result { get; set; }

        [JsonProperty("internal_operation_results")]
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
        [JsonProperty("status")]
        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonProperty("balance_updates")]
        [JsonPropertyName("balance_updates")]
        public List<IBalanceUpdate> BalanceUpdates { get; set; }

        [JsonProperty("consumed_gas")]
        [JsonPropertyName("consumed_gas")]
        public int ConsumedGas { get; set; }

        [JsonProperty("paid_storage_size_diff")]
        [JsonPropertyName("paid_storage_size_diff")]
        public int PaidStorageSizeDiff { get; set; }

        [JsonProperty("allocated_destination_contract")]
        [JsonPropertyName("allocated_destination_contract")]
        public bool AllocatedDestinationContract { get; set; }

        [JsonProperty("errors")]
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
        [JsonProperty("source")]
        [JsonPropertyName("source")]
        public string Source { get; set; }

        [JsonProperty("nonce")]
        [JsonPropertyName("nonce")]
        public int Nonce { get; set; }

        [JsonProperty("amount")]
        [JsonPropertyName("amount")]
        public long Amount { get; set; }

        [JsonProperty("destination")]
        [JsonPropertyName("destination")]
        public string Destination { get; set; }

        [JsonProperty("result")]
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

    class RawInternalDelegationResult : IInternalOperationResult
    {
        [JsonProperty("source")]
        [JsonPropertyName("source")]
        public string Source { get; set; }

        [JsonProperty("nonce")]
        [JsonPropertyName("nonce")]
        public int Nonce { get; set; }

        [JsonProperty("delegate")]
        [JsonPropertyName("delegate")]
        public string Delegate { get; set; }

        [JsonProperty("result")]
        [JsonPropertyName("result")]
        public RawDelegationContentResult Result { get; set; }

        #region validation
        public bool IsValidFormat() =>
            !string.IsNullOrEmpty(Source) &&
            Nonce >= 0 &&
            (Delegate == null || Delegate != "") &&
            Result?.IsValidFormat() == true;
        #endregion
    }

    class RawInternalOriginationResult : IInternalOperationResult
    {
        [JsonProperty("source")]
        [JsonPropertyName("source")]
        public string Source { get; set; }

        [JsonProperty("nonce")]
        [JsonPropertyName("nonce")]
        public int Nonce { get; set; }

        [JsonProperty("balance")]
        [JsonPropertyName("balance")]
        public long Balance { get; set; }

        [JsonProperty("delegate")]
        [JsonPropertyName("delegate")]
        public string Delegate { get; set; }

        [JsonProperty("script")]
        [JsonPropertyName("script")]
        public RawOriginationScript Script { get; set; }

        [JsonProperty("result")]
        [JsonPropertyName("result")]
        public RawOriginationContentResult Result { get; set; }

        #region validation
        public bool IsValidFormat() =>
            !string.IsNullOrEmpty(Source) &&
            Nonce >= 0 &&
            Balance >= 0 &&
            (Delegate == null || Delegate != "") &&
            (Script == null || Script.IsValidFormat()) &&
            Result?.IsValidFormat() == true;
        #endregion
    }
}
