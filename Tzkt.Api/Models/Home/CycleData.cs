using System;

namespace Tzkt.Api.Models.Home
{
    public class CycleData
    {
        public int Cycle { get; set; }
        public int FirstLevel { get; set; }
        public DateTime StartTime { get; set; }
        public int LastLevel { get; set; }
        public DateTime EndTime { get; set; }
        public double Progress { get; set; }
    }
}