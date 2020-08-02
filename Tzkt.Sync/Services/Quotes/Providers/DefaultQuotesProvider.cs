using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Services
{
    public class DefaultQuotesProvider : IQuoteProvider
    {
        public async Task<int> FillQuotes(IEnumerable<IQuote> quotes, IQuote last)
        {
            var filled = await Task.WhenAll(
                FillBtcQuotes(quotes, last),
                FillEurQuotes(quotes, last),
                FillUsdQuotes(quotes, last));

            return filled.Min();
        }

        async Task<int> FillBtcQuotes(IEnumerable<IQuote> quotes, IQuote last)
        {
            var res = (await GetBtc(
                quotes.First().Timestamp.AddMinutes(-30),
                quotes.Last().Timestamp)).ToList();

            if (res.Count == 0)
            {
                foreach (var quote in quotes)
                    quote.Btc = last?.Btc ?? 0;
            }
            else
            {
                var i = 0;
                foreach (var quote in quotes)
                {
                    if (quote.Timestamp < res[0].Timestamp)
                    {
                        quote.Btc = last?.Btc ?? 0;
                    }
                    else
                    {
                        while (i < res.Count - 1 && quote.Timestamp >= res[i + 1].Timestamp) i++;

                        quote.Btc = res[i].Price;
                    }
                }
            }

            return quotes.Count();
        }

        async Task<int> FillEurQuotes(IEnumerable<IQuote> quotes, IQuote last)
        {
            var res = (await GetEur(
                quotes.First().Timestamp.AddMinutes(-30),
                quotes.Last().Timestamp)).ToList();

            if (res.Count == 0)
            {
                foreach (var quote in quotes)
                    quote.Eur = last?.Eur ?? 0;
            }
            else
            {
                var i = 0;
                foreach (var quote in quotes)
                {
                    if (quote.Timestamp < res[0].Timestamp)
                    {
                        quote.Eur = last?.Eur ?? 0;
                    }
                    else
                    {
                        while (i < res.Count - 1 && quote.Timestamp >= res[i + 1].Timestamp) i++;

                        quote.Eur = res[i].Price;
                    }
                }
            }

            return quotes.Count();
        }

        async Task<int> FillUsdQuotes(IEnumerable<IQuote> quotes, IQuote last)
        {
            var res = (await GetUsd(
                quotes.First().Timestamp.AddMinutes(-30),
                quotes.Last().Timestamp)).ToList();

            if (res.Count == 0)
            {
                foreach (var quote in quotes)
                    quote.Usd = last?.Usd ?? 0;
            }
            else
            {
                var i = 0;
                foreach (var quote in quotes)
                {
                    if (quote.Timestamp < res[0].Timestamp)
                    {
                        quote.Usd = last?.Usd ?? 0;
                    }
                    else
                    {
                        while (i < res.Count - 1 && quote.Timestamp >= res[i + 1].Timestamp) i++;

                        quote.Usd = res[i].Price;
                    }
                }
            }

            return quotes.Count();
        }

        #region virtual
        public virtual Task<IEnumerable<IDefaultQuote>> GetBtc(DateTime from, DateTime to)
            => Task.FromResult(Enumerable.Empty<IDefaultQuote>());

        public virtual Task<IEnumerable<IDefaultQuote>> GetEur(DateTime from, DateTime to)
            => Task.FromResult(Enumerable.Empty<IDefaultQuote>());

        public virtual Task<IEnumerable<IDefaultQuote>> GetUsd(DateTime from, DateTime to)
            => Task.FromResult(Enumerable.Empty<IDefaultQuote>());
        #endregion
    }

    public interface IDefaultQuote
    {
        DateTime Timestamp { get; }
        double Price { get; }
    }
}
