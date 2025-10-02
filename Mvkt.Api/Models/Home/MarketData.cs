namespace Mvkt.Api.Models
{
    public class MarketData
    {
        public long TotalSupply { get; set; }
        public long CirculatingSupply { get; set; }
        public long VestingAmount { get; set; }
        public long TotalBurned { get; set; }
    }
}