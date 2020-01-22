using System.Linq;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Tzkt.Sync.Protocols.Proto6
{
    class RawProposalContent : IOperationContent
    {
        [JsonProperty("source")]
        [JsonPropertyName("source")]
        public string Source { get; set; }

        [JsonProperty("period")]
        [JsonPropertyName("period")]
        public int Period { get; set; }

        [JsonProperty("proposals")]
        [JsonPropertyName("proposals")]
        public List<string> Proposals { get; set; }

        #region validation
        public bool IsValidFormat() =>
            !string.IsNullOrEmpty(Source) &&
            Period > 0 &&
            Proposals?.Count > 0;
        #endregion
    }
}
