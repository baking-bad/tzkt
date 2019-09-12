using System;
using Newtonsoft.Json;

namespace Tzkt.Sync.Services
{
    public class Header
    {
        [JsonProperty("chain_id")]
        public string ChainId { get; set; }

        [JsonProperty("protocol")]
        public string Protocol { get; set; }

        [JsonProperty("hash")]
        public string Hash { get; set; }

        [JsonProperty("level")]
        public int Level { get; set; }

        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }
    }
}
