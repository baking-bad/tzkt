using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Tzkt.Api.Models
{
    public class QuoteShort
    {
        /// <summary>
        /// XTZ/BTC price
        /// </summary>
        public double? Btc { get; set; }

        /// <summary>
        /// XTZ/EUR price
        /// </summary>
        public double? Eur { get; set; }

        /// <summary>
        /// XTZ/USD price
        /// </summary>
        public double? Usd { get; set; }

        /// <summary>
        /// XTZ/CNY price
        /// </summary>
        public double? Cny { get; set; }

        /// <summary>
        /// XTZ/JPY price
        /// </summary>
        public double? Jpy { get; set; }

        /// <summary>
        /// XTZ/KRW price
        /// </summary>
        public double? Krw { get; set; }

        /// <summary>
        /// XTZ/ETH price
        /// </summary>
        public double? Eth { get; set; }

        /// <summary>
        /// XTZ/GBP price
        /// </summary>
        public double? Gbp { get; set; }
    }

    [Flags]
    [JsonConverter(typeof(StringEnumConverter))]
    public enum Symbols
    {
        None = 0,
        Btc = 1,
        Eur = 2,
        Usd = 4,
        Cny = 8,
        Jpy = 16,
        Krw = 32,
        Eth = 64,
        Gbp = 128
    }
}
