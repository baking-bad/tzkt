using System.Collections.Generic;

namespace Tzkt.Api.Models
{
    public class HomeStats
    {
        public DailyData DailyData { get; set; }
        
        public CycleData CycleData { get; set; }
        
        public GovernanceData GovernanceData { get; set; }
        
        public StakingData StakingData { get; set; }
        public List<ChartPoint> TotalStakingChart { get; set; }
        
        public ContractsData ContractsData { get; set; }
        public List<ChartPoint> TotalCallsChart { get; set; }
        
        public AccountsData AccountsData { get; set; }
        public List<ChartPoint> TotalAccountsChart { get; set; }
        
        public TxsData TxsData { get; set; }
        public List<ChartPoint> TotalTxsChart { get; set; }

        public MarketData MarketData { get; set; }
        public List<ChartPoint<double>> PriceChart { get; set; }
    }
}