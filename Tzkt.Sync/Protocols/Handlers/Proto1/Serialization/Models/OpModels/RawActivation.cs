using System.Linq;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Tzkt.Sync.Protocols.Proto1
{
    class RawActivationContent : IOperationContent
    {
        [JsonPropertyName("pkh")]
        public string Address { get; set; }

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
        [JsonPropertyName("balance_updates")]
        public List<IBalanceUpdate> BalanceUpdates { get; set; }

        #region validation
        public bool IsValidFormat() =>
            BalanceUpdates?.Count > 0 &&
            BalanceUpdates.All(x => x.IsValidFormat());
        #endregion
    }
}
