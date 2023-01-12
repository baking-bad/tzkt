using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Dapper;
using Tzkt.Api.Models;

namespace Tzkt.Api.Services.Cache
{
    public class QuotesCache : DbConnection
    {
        readonly List<double>[] Quotes;
        readonly StateCache State;
        readonly ILogger Logger;

        public QuotesCache(StateCache state, IConfiguration config, ILogger<QuotesCache> logger) : base(config)
        {
            logger.LogDebug("Initializing quotes cache...");

            Quotes = new List<double>[8];
            State = state;
            Logger = logger;

            var sql = @"
                SELECT    ""Btc"", ""Eur"", ""Usd"", ""Cny"", ""Jpy"", ""Krw"", ""Eth"", ""Gbp""
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
                Quotes[6].Add(row.Eth);
                Quotes[7].Add(row.Gbp);
            }

            logger.LogInformation("Loaded {cnt} quotes", Quotes[0].Count);
        }

        public async Task UpdateAsync()
        {
            Logger.LogDebug("Updating quotes cache");
            var sql = $@"
                SELECT    ""Level"", ""Btc"", ""Eur"", ""Usd"", ""Cny"", ""Jpy"", ""Krw"", ""Eth"", ""Gbp""
                FROM      ""Quotes""
                WHERE     ""Level"" > @fromLevel
                ORDER BY  ""Level""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { fromLevel = Math.Min(Quotes[0].Count - 1, State.ValidLevel) });

            foreach (var row in rows)
            {
                if (row.Level < Quotes[0].Count)
                {
                    Quotes[0][row.Level] = row.Btc;
                    Quotes[1][row.Level] = row.Eur;
                    Quotes[2][row.Level] = row.Usd;
                    Quotes[3][row.Level] = row.Cny;
                    Quotes[4][row.Level] = row.Jpy;
                    Quotes[5][row.Level] = row.Krw;
                    Quotes[6][row.Level] = row.Eth;
                    Quotes[7][row.Level] = row.Gbp;
                }
                else
                {
                    Quotes[0].Add(row.Btc);
                    Quotes[1].Add(row.Eur);
                    Quotes[2].Add(row.Usd);
                    Quotes[3].Add(row.Cny);
                    Quotes[4].Add(row.Jpy);
                    Quotes[5].Add(row.Krw);
                    Quotes[6].Add(row.Eth);
                    Quotes[7].Add(row.Gbp);
                }
            }
            Logger.LogDebug("{cnt} quotes updates", rows.Count());
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

                if (symbols.HasFlag(Symbols.Eth))
                    quote.Eth = Quotes[6][^1];

                if (symbols.HasFlag(Symbols.Gbp))
                    quote.Gbp = Quotes[7][^1];
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

                if (symbols.HasFlag(Symbols.Eth))
                    quote.Eth = Quotes[6][level];

                if (symbols.HasFlag(Symbols.Gbp))
                    quote.Gbp = Quotes[7][level];
            }

            return quote;
        }
    }
}
