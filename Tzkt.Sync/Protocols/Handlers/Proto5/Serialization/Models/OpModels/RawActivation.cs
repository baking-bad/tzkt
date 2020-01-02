using System.Linq;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Tzkt.Sync.Protocols.Proto5
{
    class RawActivationContent : IOperationContent
    {
        [JsonProperty("pkh")]
        [JsonPropertyName("pkh")]
        public string Address { get; set; }

        [JsonProperty("metadata")]
        [JsonPropertyName("metadata")]
        public RawActivationContentMetadata Metadata { get; set; }

        #region validation
        public bool IsValidFormat() =>
            !string.IsNullOrEmpty(Address) &&
            Metadata?.IsValidFormat() == true;
        #endregion
    }

    class RawActivationContentMetadata
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
