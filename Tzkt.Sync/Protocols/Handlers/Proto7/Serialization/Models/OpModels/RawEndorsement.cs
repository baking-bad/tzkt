using System.Linq;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Tzkt.Sync.Protocols.Proto7
{
    class RawEndorsementContent : IOperationContent
    {
        [JsonProperty("level")]
        [JsonPropertyName("level")]
        public int Level { get; set; }

        [JsonProperty("metadata")]
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
        [JsonProperty("balance_updates")]
        [JsonPropertyName("balance_updates")]
        public List<IBalanceUpdate> BalanceUpdates { get; set; }

        [JsonProperty("delegate")]
        [JsonPropertyName("delegate")]
        public string Delegate { get; set; }

        [JsonProperty("slots")]
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
