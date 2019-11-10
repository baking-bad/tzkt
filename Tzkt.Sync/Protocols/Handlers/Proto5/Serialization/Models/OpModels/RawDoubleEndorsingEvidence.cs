using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Tzkt.Sync.Protocols.Proto5
{
    class RawDoubleEndorsingEvidenceContent : IOperationContent
    {
        [JsonPropertyName("op1")]
        public RawDoubleEndorsingEvidenceOp Op1 { get; set; }

        [JsonPropertyName("op2")]
        public RawDoubleEndorsingEvidenceOp Op2 { get; set; }

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
        [JsonPropertyName("branch")]
        public string Branch { get; set; }

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
        [JsonPropertyName("kind")]
        public string Kind { get; set; }

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
        [JsonPropertyName("balance_updates")]
        public List<IBalanceUpdate> BalanceUpdates { get; set; }

        #region validation
        public bool IsValidFormat() =>
            BalanceUpdates?.Count > 0 &&
            BalanceUpdates.All(x => x.IsValidFormat());
        #endregion
    }
}
