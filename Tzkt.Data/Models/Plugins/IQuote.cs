using System;

namespace Tzkt.Data.Models
{
    public interface IQuote
    {
        int Level { get; }
        DateTime Timestamp { get; }

        double Btc { get; set; }
        double Eur { get; set; }
        double Usd { get; set; }
        double Cny { get; set; }
        double Jpy { get; set; }
        double Krw { get; set; }
        double Eth { get; set; }
        double Gbp { get; set; }
    }
}
