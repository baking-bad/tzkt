using System;

namespace Tzkt.Api.Models.Home
{
    public class CycleData
    {
        public int CurrentCycle { get; set; }
        public int FirstLevel { get; set; }
        public int LastLevel { get; set; }
        public int Progress { get; set; }
        public DateTime CycleEndDate { get; set; }
    }
}