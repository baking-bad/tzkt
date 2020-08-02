using System;
using System.Collections.Generic;
using System.Text;

namespace Tzkt.Data.Models
{
    public interface IQuote
    {
        int Level { get; }
        DateTime Timestamp { get; }

        double Btc { get; set; }
        double Eur { get; set; }
        double Usd { get; set; }
    }
}
