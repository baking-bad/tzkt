using System;

namespace Tzkt.Api.Models
{
    public class Quote
    {
        /// <summary>
        /// The level of the block at which the quote has been saved
        /// </summary>
        public int Level { get; set; }
        
        /// <summary>
        /// The datetime at which the quote has been saved (ISO 8601, e.g. `2020-02-20T02:40:57Z`)
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// XTZ/BTC price
        /// </summary>
        public double Btc { get; set; }

        /// <summary>
        /// XTZ/EUR price
        /// </summary>
        public double Eur { get; set; }

        /// <summary>
        /// XTZ/USD price
        /// </summary>
        public double Usd { get; set; }
    }
}
