using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Tzkt.Api.Models.Send
{
    public class PreapplyResponse
    {
        [JsonPropertyName("contents")]
        public List<Content> Contents { get; set; }

        [JsonPropertyName("signature")]
        public string Signature { get; set; }
        
    public class BalanceUpdate
    {
        [JsonPropertyName("kind")]
        public string Kind { get; set; }

        [JsonPropertyName("contract")]
        public string Contract { get; set; }

        [JsonPropertyName("change")]
        public string Change { get; set; }

        [JsonPropertyName("origin")]
        public string Origin { get; set; }

        [JsonPropertyName("category")]
        public string Category { get; set; }

        [JsonPropertyName("delegate")]
        public string Delegate { get; set; }

        [JsonPropertyName("cycle")]
        public int? Cycle { get; set; }
    }

    public class Error
    {
        [JsonPropertyName("kind")]
        public string Kind { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }
    }

    public class OperationResult
    {
        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("consumed_gas")]
        public string ConsumedGas { get; set; }

        [JsonPropertyName("consumed_milligas")]
        public string ConsumedMilligas { get; set; }

        [JsonPropertyName("errors")]
        public List<Error> Errors { get; set; }
    }

    public class Metadata
    {
        [JsonPropertyName("balance_updates")]
        public List<BalanceUpdate> BalanceUpdates { get; set; }

        [JsonPropertyName("operation_result")]
        public OperationResult OperationResult { get; set; }
    }

    public class Content
    {
        [JsonPropertyName("kind")]
        public string Kind { get; set; }

        [JsonPropertyName("source")]
        public string Source { get; set; }

        [JsonPropertyName("fee")]
        public string Fee { get; set; }

        [JsonPropertyName("counter")]
        public string Counter { get; set; }

        [JsonPropertyName("gas_limit")]
        public string GasLimit { get; set; }

        [JsonPropertyName("storage_limit")]
        public string StorageLimit { get; set; }

        [JsonPropertyName("public_key")]
        public string PublicKey { get; set; }

        [JsonPropertyName("metadata")]
        public Metadata Metadata { get; set; }

        [JsonPropertyName("amount")]
        public string Amount { get; set; }

        [JsonPropertyName("destination")]
        public string Destination { get; set; }
    }
    }

}