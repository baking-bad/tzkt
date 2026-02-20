namespace Mvkt.Api.Models
{
    public class NetworkRewardsData
    {
        public long TotalRewardsAllTime { get; set; }
        public long TotalBlockRewards { get; set; }
        public long TotalEndorsementRewards { get; set; }
        public long TotalBlockFees { get; set; }
        public int CyclesCount { get; set; }
        public double AverageRewardsPerCycle { get; set; }
        public List<CycleRewardSummary> CycleRewardSummaries { get; set; } = new();
    }

    public class CycleRewardSummary
    {
        public int Cycle { get; set; }
        public long TotalBlockRewards { get; set; }
        public long TotalEndorsementRewards { get; set; }
        public long TotalBlockFees { get; set; }
        public long TotalRewards { get; set; }
        public int ActiveBakers { get; set; }
    }
}
