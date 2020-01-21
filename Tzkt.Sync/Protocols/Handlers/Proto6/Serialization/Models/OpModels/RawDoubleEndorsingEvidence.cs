using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Tzkt.Sync.Protocols.Proto6
{
    class RawDoubleEndorsingEvidenceContent : IOperationContent
    {
        [JsonProperty("op1")]
        [JsonPropertyName("op1")]
        public RawDoubleEndorsingEvidenceOp Op1 { get; set; }

        [JsonProperty("op2")]
        [JsonPropertyName("op2")]
        public RawDoubleEndorsingEvidenceOp Op2 { get; set; }

        [JsonProperty("metadata")]
        [JsonPropertyName("metadata")]
        public RawDoubleEndorsingEvidenceContentMetadata Metadata { get; set; }

        #region validation
        public bool IsValidFormat() =>
            Op1?.IsValidFormat() == true &&
            Op2?.IsValidFormat() == true &&
            Metadata?.IsValidFormat() == true;
        #endregion
    }

    class RawDoubleEndorsingEvidenceOp
    {
        [JsonProperty("branch")]
        [JsonPropertyName("branch")]
        public string Branch { get; set; }

        [JsonProperty("operations")]
        [JsonPropertyName("operations")]
        public RawDoubleEndorsingEvidenceOpEndorsement Operations { get; set; }

        #region validation
        public bool IsValidFormat() =>
            !string.IsNullOrEmpty(Branch) &&
            Operations?.IsValidFormat() == true;
        #endregion
    }

    class RawDoubleEndorsingEvidenceOpEndorsement
    {
        [JsonProperty("kind")]
        [JsonPropertyName("kind")]
        public string Kind { get; set; }

        [JsonProperty("level")]
        [JsonPropertyName("level")]
        public int Level { get; set; }

        #region validation
        public bool IsValidFormat() =>
            !string.IsNullOrEmpty(Kind) &&
            Level >= 0;
        #endregion
    }

    class RawDoubleEndorsingEvidenceContentMetadata
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
