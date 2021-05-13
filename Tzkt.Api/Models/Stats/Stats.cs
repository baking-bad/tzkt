using System.Collections.Generic;

namespace Tzkt.Api.Models
{
    public class Stats
    {
        public DailyData DailyData { get; set; }
        
        public CycleData CycleData { get; set; }
        
        public GovernanceData GovernanceData { get; set; }
        
        public StakingData StakingData { get; set; }
        public List<ChartPoint> StakingChart { get; set; }
        
        public ContractsData ContractsData { get; set; }
        public List<ChartPoint> ContractsChart { get; set; }
        
        public AccountsData AccountsData { get; set; }
        public List<ChartPoint> AccountsChart { get; set; }
        
        public TxsData TxsData { get; set; }
        public List<ChartPoint> TxsChart { get; set; }

        public MarketData MarketData { get; set; }
        public List<ChartPoint<Quote>> MarketChart { get; set; }
    }
}