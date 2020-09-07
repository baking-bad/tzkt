using System.Linq;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Tzkt.Sync.Protocols.Proto7
{
    class RawBallotContent : IOperationContent
    {
        [JsonProperty("source")]
        [JsonPropertyName("source")]
        public string Source { get; set; }

        [JsonProperty("period")]
        [JsonPropertyName("period")]
        public int Period { get; set; }

        [JsonProperty("proposal")]
        [JsonPropertyName("proposal")]
        public string Proposal { get; set; }

        [JsonProperty("ballot")]
        [JsonPropertyName("ballot")]
        public string Ballot { get; set; }

        #region validation
        public bool IsValidFormat() =>
            !string.IsNullOrEmpty(Source) &&
            Period > 0 &&
            !string.IsNullOrEmpty(Proposal) &&
            !string.IsNullOrEmpty(Ballot);
        #endregion
    }
}
