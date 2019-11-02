using System.Linq;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Tzkt.Sync.Protocols.Proto3
{
    class RawEndorsementContent : IOperationContent
    {
        [JsonPropertyName("level")]
        public int Level { get; set; }

        [JsonPropertyName("metadata")]
        public RawEndorsementContentMetadata Metadata { get; set; }

        #region validation
        public bool IsValidFormat() =>
            Level >= 0 &&
            Metadata?.IsValidFormat() == true;
        #endregion
    }

    class RawEndorsementContentMetadata
    {
        [JsonPropertyName("balance_updates")]
        public List<IBalanceUpdate> BalanceUpdates { get; set; }

        [JsonPropertyName("delegate")]
        public string Delegate { get; set; }

        [JsonPropertyName("slots")]
        public List<int> Slots { get; set; }

        #region validation
        public bool IsValidFormat() =>
            BalanceUpdates != null &&
            BalanceUpdates.All(x => x.IsValidFormat()) &&
            !string.IsNullOrEmpty(Delegate) &&
            Slots?.Count > 0;
        #endregion
    }
}
