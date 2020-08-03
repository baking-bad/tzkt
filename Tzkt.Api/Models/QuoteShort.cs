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
    }

    [Flags]
    [JsonConverter(typeof(StringEnumConverter))]
    public enum Symbols
    {
        None = 0,
        Btc = 1,
        Eur = 2,
        Usd = 4
    }
}
