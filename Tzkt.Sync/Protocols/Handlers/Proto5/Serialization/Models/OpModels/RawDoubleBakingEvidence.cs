using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Tzkt.Sync.Protocols.Proto5
{
    class RawDoubleBakingEvidenceContent : IOperationContent
    {
        [JsonPropertyName("bh1")]
        public RawDoubleBakingEvidenceBlockHeader Block1 { get; set; }

        [JsonPropertyName("bh2")]
        public RawDoubleBakingEvidenceBlockHeader Block2 { get; set; }

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
        [JsonPropertyName("level")]
        public int Level { get; set; }

        [JsonPropertyName("predecessor")]
        public string Predecessor { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

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
        [JsonPropertyName("balance_updates")]
        public List<IBalanceUpdate> BalanceUpdates { get; set; }

        #region validation
        public bool IsValidFormat() =>
            BalanceUpdates?.Count > 0 &&
            BalanceUpdates.All(x => x.IsValidFormat());
        #endregion
    }
}
