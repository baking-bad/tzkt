using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Tzkt.Sync.Protocols.Proto6
{
    class RawDoubleBakingEvidenceContent : IOperationContent
    {
        [JsonProperty("bh1")]
        [JsonPropertyName("bh1")]
        public RawDoubleBakingEvidenceBlockHeader Block1 { get; set; }

        [JsonProperty("bh2")]
        [JsonPropertyName("bh2")]
        public RawDoubleBakingEvidenceBlockHeader Block2 { get; set; }

        [JsonProperty("metadata")]
        [JsonPropertyName("metadata")]
        public RawDoubleBakingEvidenceContentMetadata Metadata { get; set; }

        #region validation
        public bool IsValidFormat() =>
            Block1?.IsValidFormat() == true &&
            Block2?.IsValidFormat() == true &&
            Metadata?.IsValidFormat() == true;
        #endregion
    }

    class RawDoubleBakingEvidenceBlockHeader
    {
        [JsonProperty("level")]
        [JsonPropertyName("level")]
        public int Level { get; set; }

        [JsonProperty("predecessor")]
        [JsonPropertyName("predecessor")]
        public string Predecessor { get; set; }

        [JsonProperty("timestamp")]
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonProperty("priority")]
        [JsonPropertyName("priority")]
        public int Priority { get; set; }

        #region validation
        public bool IsValidFormat() =>
            Level >= 0 &&
            !string.IsNullOrEmpty(Predecessor) &&
            Timestamp != DateTime.MinValue &&
            Priority >= 0;
        #endregion
    }

    class RawDoubleBakingEvidenceContentMetadata
    {
        [JsonProperty("balance_updates")]
        [JsonPropertyName("balance_updates")]
        public List<IBalanceUpdate> BalanceUpdates { get; set; }

        #region validation
        public bool IsValidFormat() =>
            BalanceUpdates?.Count > 0 &&
            BalanceUpdates.All(x => x.IsValidFormat());
        #endregion
    }
}
