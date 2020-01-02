using System.Linq;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Tzkt.Sync.Protocols.Proto5
{
    class RawNonceRevelationContent : IOperationContent
    {
        [JsonProperty("level")]
        [JsonPropertyName("level")]
        public int Level { get; set; }

        [JsonProperty("metadata")]
        [JsonPropertyName("metadata")]
        public RawNonceRevelationContentMetadata Metadata { get; set; }

        #region validation
        public bool IsValidFormat() =>
            Level >= 0 &&
            Metadata?.IsValidFormat() == true;
        #endregion
    }

    class RawNonceRevelationContentMetadata
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
