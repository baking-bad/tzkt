using System.Linq;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Tzkt.Sync.Protocols.Proto4
{
    class RawNonceRevelationContent : IOperationContent
    {
        [JsonPropertyName("level")]
        public int Level { get; set; }

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
        [JsonPropertyName("balance_updates")]
        public List<IBalanceUpdate> BalanceUpdates { get; set; }

        #region validation
        public bool IsValidFormat() =>
            BalanceUpdates?.Count > 0 &&
            BalanceUpdates.All(x => x.IsValidFormat());
        #endregion
    }
}
