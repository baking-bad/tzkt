using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Tzkt.Sync.Services
{
    public class Constants
    {
        [JsonPropertyName("blocks_per_cycle")]
        public int CycleLength { get; set; }

        [JsonPropertyName("time_between_blocks")]
        public List<int> BlockIntervals { get; set; }

        [JsonPropertyName("minimal_block_delay")]
        public int? MinBlockDelay { get; set; }
    }
}
