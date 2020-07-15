using System;

namespace Tzkt.Api.Models
{
    public class BakingInterval
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int Blocks { get; set; }
        public int Slots { get; set; }
        public int? FirstLevel { get; set; }
        public int? LastLevel { get; set; }
        public int? Status { get; set; }
    }
}
