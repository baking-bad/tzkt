using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Dapper;

using Tzkt.Api.Models;

namespace Tzkt.Api.Services.Cache
{
    public class QuotesCache : DbConnection
    {
        readonly List<double>[] Quotes;
        readonly ILogger Logger;

        public QuotesCache(IConfiguration config, ILogger<QuotesCache> logger) : base(config)
        {
            Quotes = new List<double>[6];
            Logger = logger;

            Logger.LogDebug("Initializing quotes cache...");

            var sql = @"
                SELECT    ""Btc"", ""Eur"", ""Usd"", ""Cny"", ""Jpy"", ""Krw""
                FROM      ""Quotes""
                ORDER BY  ""Level""";

            using var db = GetConnection();
            var rows = db.Query(sql);

            var cnt = rows.Count();
            for (int i = 0; i < Quotes.Length; i++)
                Quotes[i] = new List<double>(cnt + 130_000);

            foreach (var row in rows)
            {
                Quotes[0].Add(row.Btc);
                Quotes[1].Add(row.Eur);
                Quotes[2].Add(row.Usd);
                Quotes[3].Add(row.Cny);
                Quotes[4].Add(row.Jpy);
                Quotes[5].Add(row.Krw);
            }

            Logger.LogDebug($"Quotes cache initialized with {Quotes[0].Count} items");
        }

        public double Get(int symbol)
        {
            return Quotes[symbol][^1];
        }

        public double Get(int symbol, int level)
        {
            return level >= Quotes[symbol].Count ? Quotes[symbol][^1] : Quotes[symbol][level];
        }

        public QuoteShort Get(Symbols symbols, int level)
        {
            if (symbols == Symbols.None)
                return null;

            var quote = new QuoteShort();

            if (level >= Quotes[0].Count)
            {
                if (symbols.HasFlag(Symbols.Btc))
                    quote.Btc = Quotes[0][^1];

                if (symbols.HasFlag(Symbols.Eur))
                    quote.Eur = Quotes[1][^1];

                if (symbols.HasFlag(Symbols.Usd))
                    quote.Usd = Quotes[2][^1];

                if (symbols.HasFlag(Symbols.Cny))
                    quote.Cny = Quotes[3][^1];

                if (symbols.HasFlag(Symbols.Jpy))
                    quote.Jpy = Quotes[4][^1];

                if (symbols.HasFlag(Symbols.Krw))
                    quote.Krw = Quotes[5][^1];
            }
            else
            {
                if (symbols.HasFlag(Symbols.Btc))
                    quote.Btc = Quotes[0][level];

                if (symbols.HasFlag(Symbols.Eur))
                    quote.Eur = Quotes[1][level];

                if (symbols.HasFlag(Symbols.Usd))
                    quote.Usd = Quotes[2][level];

                if (symbols.HasFlag(Symbols.Cny))
                    quote.Cny = Quotes[3][level];

                if (symbols.HasFlag(Symbols.Jpy))
                    quote.Jpy = Quotes[4][level];

                if (symbols.HasFlag(Symbols.Krw))
                    quote.Krw = Quotes[5][level];
            }

            return quote;
        }

        public async Task UpdateAsync()
        {
            var sql = $@"
                SELECT    ""Btc"", ""Eur"", ""Usd"", ""Cny"", ""Jpy"", ""Krw""
                FROM      ""Quotes""
                WHERE     ""Level"" >= {Quotes[0].Count}
                ORDER BY  ""Level""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql);

            Quotes[0].AddRange(rows.Select(x => (double)x.Btc));
            Quotes[1].AddRange(rows.Select(x => (double)x.Eur));
            Quotes[2].AddRange(rows.Select(x => (double)x.Usd));
            Quotes[3].AddRange(rows.Select(x => (double)x.Cny));
            Quotes[4].AddRange(rows.Select(x => (double)x.Jpy));
            Quotes[5].AddRange(rows.Select(x => (double)x.Krw));
        }
    }

    public static class QuotesCacheExt
    {
        public static void AddQuotesCache(this IServiceCollection services)
        {
            services.AddSingleton<QuotesCache>();
        }
    }
}
