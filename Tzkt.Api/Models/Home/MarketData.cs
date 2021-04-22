using System.Collections.Generic;

namespace Tzkt.Api.Models.Home
{
    public class MarketData
    {
        public long TotalSupply { get; set; }
        public long CirculationSupply { get; set; }
        public Quote Quote { get; set; }
        public IEnumerable<Quote> PriceData { get; set; }
    }
}