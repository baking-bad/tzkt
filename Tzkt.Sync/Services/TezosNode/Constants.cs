using System.Collections.Generic;
using Newtonsoft.Json;

namespace Tzkt.Sync.Services
{
    public class Constants
    {
        [JsonProperty("blocks_per_cycle")]
        public int CycleLength { get; set; }

        [JsonProperty("time_between_blocks")]
        public List<int> BlockIntervals { get; set; }
    }
}
