using System.Text.Json.Serialization;

namespace Tzkt.Sync.Services
{
    public class Header
    {
        [JsonPropertyName("chain_id")]
        public required string ChainId { get; set; }

        [JsonPropertyName("protocol")]
        public required string Protocol { get; set; }

        [JsonPropertyName("predecessor")]
        public required string Predecessor { get; set; }

        [JsonPropertyName("hash")]
        public required string Hash { get; set; }

        [JsonPropertyName("level")]
        public required int Level { get; set; }

        [JsonPropertyName("timestamp")]
        public required DateTime Timestamp { get; set; }
    }
}
