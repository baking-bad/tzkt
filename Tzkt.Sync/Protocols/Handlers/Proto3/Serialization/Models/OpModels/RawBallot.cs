using System.Linq;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Tzkt.Sync.Protocols.Proto3
{
    class RawBallotContent : IOperationContent
    {
        [JsonPropertyName("source")]
        public string Source { get; set; }

        [JsonPropertyName("period")]
        public int Period { get; set; }

        [JsonPropertyName("proposal")]
        public string Proposal { get; set; }

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
