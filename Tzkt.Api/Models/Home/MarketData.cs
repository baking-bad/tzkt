namespace Tzkt.Api.Models.Home
{
    public class MarketData
    {
        public long TotalSupply { get; set; }
        public long CirculatingSupply { get; set; }
        public Quote Quote { get; set; }
    }
}