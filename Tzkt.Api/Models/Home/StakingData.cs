using System.Collections.Generic;

namespace Tzkt.Api.Models.Home
{
    public class StakingData
    {
        public List<ChartPoint> Chart { get; set; }

        public long TotalStaking { get; set; }
        public int StakingPercentage { get; set; }
        public double AvgRoi { get; set; }
        public double Inflation { get; set; }
        public int BakersCount { get; set; }
    }
}