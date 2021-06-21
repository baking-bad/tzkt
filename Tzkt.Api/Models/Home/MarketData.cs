namespace Tzkt.Api.Models
{
    public class MarketData
    {
        public long TotalSupply { get; set; }
        public long CirculatingSupply { get; set; }
        public Quote Quote { get; set; }
        public Quote PrevQuote { get; set; }
    }
}