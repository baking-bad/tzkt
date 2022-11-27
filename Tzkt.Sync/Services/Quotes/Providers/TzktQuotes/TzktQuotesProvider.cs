using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Services
{
    class TzktQuotesProvider : IQuoteProvider, IDisposable
    {
        public const string ProviderName = "TzktQuotes";

        readonly TzktClient Client;
        readonly TzktQuotesProviderConfig Config;

        public TzktQuotesProvider(IConfiguration config)
        {
            Config = config.GetTzktQuotesProviderConfig();
            Client = new TzktClient(Config.BaseUrl, Config.Timeout);
        }

        public void Dispose() => Client.Dispose();

        public async Task<int> FillQuotes(IEnumerable<IQuote> quotes, IQuote last)
        {
            var res = await GetQuotes(
                quotes.First().Timestamp.AddMinutes(-30),
                quotes.Last().Timestamp);
            
            if (res.Count == 0)
            {
                foreach (var quote in quotes)
                {
                    quote.Btc = last?.Btc ?? 0;
                    quote.Eur = last?.Eur ?? 0;
                    quote.Usd = last?.Usd ?? 0;
                    quote.Cny = last?.Cny ?? 0;
                    quote.Jpy = last?.Jpy ?? 0;
                    quote.Krw = last?.Krw ?? 0;
                    quote.Eth = last?.Eth ?? 0;
                    quote.Gbp = last?.Gbp ?? 0;
                }
            }
            else
            {
                var i = 0;
                foreach (var quote in quotes)
                {
                    if (quote.Timestamp < res[0].Timestamp)
                    {
                        quote.Btc = last?.Btc ?? 0;
                        quote.Eur = last?.Eur ?? 0;
                        quote.Usd = last?.Usd ?? 0;
                        quote.Cny = last?.Cny ?? 0;
                        quote.Jpy = last?.Jpy ?? 0;
                        quote.Krw = last?.Krw ?? 0;
                        quote.Eth = last?.Eth ?? 0;
                        quote.Gbp = last?.Gbp ?? 0;
                    }
                    else
                    {
                        while (i < res.Count - 1 && quote.Timestamp >= res[i + 1].Timestamp) i++;

                        quote.Btc = res[i].Btc;
                        quote.Eur = res[i].Eur;
                        quote.Usd = res[i].Usd;
                        quote.Cny = res[i].Cny;
                        quote.Jpy = res[i].Jpy;
                        quote.Krw = res[i].Krw;
                        quote.Eth = res[i].Eth;
                        quote.Gbp = res[i].Gbp;
                    }
                }
            }

            return quotes.Count();
        }

        async Task<List<TzktQuote>> GetQuotes(DateTime from, DateTime to)
        {
            var res = await Client.GetObjectAsync<List<TzktQuote>>(
                $"quotes?from={from:yyyy-MM-ddTHH:mm:ssZ}&to={to:yyyy-MM-ddTHH:mm:ssZ}&limit=10000");

            while (res.Count > 0 && res.Count % 10000 == 0)
                res.AddRange(await Client.GetObjectAsync<List<TzktQuote>>(
                    $"quotes?from={res[^1].Timestamp.AddSeconds(1):yyyy-MM-ddTHH:mm:ssZ}&to={to:yyyy-MM-ddTHH:mm:ssZ}&limit=10000"));

            return res;
        }
    }

    public class TzktQuote : IQuote
    {
        [JsonPropertyName("timestamp")]
        [JsonConverter(typeof(JsonDateTimeConverter))]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("btc")]
        public double Btc { get; set; }

        [JsonPropertyName("eur")]
        public double Eur { get; set; }

        [JsonPropertyName("usd")]
        public double Usd { get; set; }

        [JsonPropertyName("cny")]
        public double Cny { get; set; }

        [JsonPropertyName("jpy")]
        public double Jpy { get; set; }

        [JsonPropertyName("krw")]
        public double Krw { get; set; }

        [JsonPropertyName("eth")]
        public double Eth { get; set; }

        [JsonPropertyName("gbp")]
        public double Gbp { get; set; }

        int IQuote.Level => throw new NotImplementedException();
    }
}
