namespace Tzkt.Api.Models
{
    public class HomeStats
    {
        public required DailyData DailyData { get; set; }
        public required CycleData CycleData { get; set; }
        public required GovernanceData GovernanceData { get; set; }
        public required StakingData StakingData { get; set; }
        public required AccountsData AccountsData { get; set; }
        public required TxsData TxsData { get; set; }
        public required MarketData MarketData { get; set; }
        public required List<ChartPoint<double>> PriceChart { get; set; }
    }
}