using System;
using System.Text.Json.Serialization;

namespace Tzkt.Sync.Services
{
    public class Header
    {
        [JsonPropertyName("chain_id")]
        public string ChainId { get; set; }

        [JsonPropertyName("protocol")]
        public string Protocol { get; set; }

        [JsonPropertyName("predecessor")]
        public string Predecessor { get; set; }

        [JsonPropertyName("hash")]
        public string Hash { get; set; }

        [JsonPropertyName("level")]
        public int Level { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }
    }
}
