using System;

namespace Tzkt.Api.Models
{
    public class BakingRight
    {
        public string Type { get; set; }

        public int Cycle { get; set; }

        public int Level { get; set; }

        public DateTime Timestamp { get; set; }

        public int? Slots { get; set; }

        public int? Priority { get; set; }

        public Alias Baker { get; set; }

        public string Status { get; set; }
    }
}
