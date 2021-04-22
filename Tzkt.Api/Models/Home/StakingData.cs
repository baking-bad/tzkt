using System.Collections.Generic;

namespace Tzkt.Api.Models.Home
{
    public class StakingData
    {
        //TODO Collect 12 month amount of total staking
        public IEnumerable<ChartPoint> Chart { get; set; }

        public long TotalStaking { get; set; }
        public int StakingPercentage { get; set; }
        public decimal AvgRoi { get; set; }
        public decimal Inflation { get; set; }
        public int BakersCount { get; set; }
    }
}