namespace Tzkt.Api.Models.Home
{
    public class StakingData
    {
        public long TotalStaking { get; set; }
        public double StakingPercentage { get; set; }
        public double AvgRoi { get; set; }
        public double Inflation { get; set; }
        public int Bakers { get; set; }
    }
}