using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace Tzkt.Api.Services.Metadata
{
    public class ProtocolMetadata
    {
        [JsonPropertyName("hash")]
        public string Hash { get; set; }

        [JsonPropertyName("alias")]
        public string Alias { get; set; }

        [JsonPropertyName("docs")]
        public string Docs { get; set; }

        public override string ToString() => Alias ?? Hash;
    }
}
