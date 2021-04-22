namespace Tzkt.Api.Models.Home
{
    public class HomeData
    {
        public HeaderData HeaderData { get; set; }
        public CycleData CycleData { get; set; }
        public TxsData TxsData { get; set; }
        public StakingData StakingData { get; set; }
        public ContractsData ContractsData { get; set; }
        public MarketData MarketData { get; set; }
        public GovernanceData GovernanceData { get; set; }
    }
}