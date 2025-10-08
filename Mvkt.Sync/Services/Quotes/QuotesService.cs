using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

using Mvkt.Data;
using Mvkt.Data.Models;

namespace Mvkt.Sync.Services
{
    public class QuotesService
    {
        #region settings
        const int Chunk = 100_000;
        const int CacheSize = 10_000;
        #endregion

        readonly MvktContext Db;
        readonly CacheService Cache;
        readonly IQuoteProvider Provider;
        readonly QuotesServiceConfig Config;
        readonly ILogger Logger;

        public QuotesService(MvktContext db, CacheService cache, IQuoteProvider provider, IConfiguration config, ILogger<QuotesService> logger)
        {
            Db = db;
            Cache = cache;
            Provider = provider;
            Config = config.GetQuotesServiceConfig();
            Logger = logger;
        }

        public async Task Init()
        {
            Logger.LogInformation($"Quote provider: {Provider.GetType().Name} ({(Config.Async ? "Async" : "Sync")})");

            var state = Cache.AppState.Get();
            
            if (state.QuoteLevel < state.Level)
            {
                try
                {
                    Logger.LogDebug($"{state.Level - state.QuoteLevel} quotes missed. Start sync...");
                    while (state.QuoteLevel < state.Level)
                    {
                        var quotes = await Db.Blocks
                            .AsNoTracking()
                            .Where(x => x.Level > state.QuoteLevel)
                            .OrderBy(x => x.Level)
                            .Take(Chunk)
                            .Select(x => new Quote
                            {
                                Level = x.Level,
                                Timestamp = x.Timestamp
                            })
                            .ToListAsync();

                        if (quotes.Count == 0) break;

                        var filled = await Provider.FillQuotes(quotes, LastQuote(state));
                        if (filled == 0) throw new Exception("0 quotes filled");

                        using var tx = await Db.Database.BeginTransactionAsync();
                        try
                        {
                            SaveQuotes(filled == quotes.Count ? quotes : quotes.Take(filled));
                            UpdateState(state, quotes[filled - 1]);

                            await tx.CommitAsync();
                            Logger.LogDebug($"{filled} quotes added");
                        }
                        catch
                        {
                            await tx.RollbackAsync();
                            throw;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to sync quotes");
                }
            }
        }

        public async Task Commit()
        {
            try
            {
                var state = Cache.AppState.Get();
                
                if (state.Level - state.QuoteLevel < CacheSize)
                {
                    var quotes = new List<Quote>(state.Level - state.QuoteLevel);
                    for (int level = state.QuoteLevel + 1; level <= state.Level; level++)
                    {
                        var ts = (await Cache.Blocks.GetAsync(level)).Timestamp;
                        quotes.Add(new Quote
                        {
                            Level = level,
                            Timestamp = ts
                        });
                    }

                    if (quotes.Count == 0) return;

                    // If all new quotes are within the same minute as the last saved quote,
                    // reuse last values without hitting the external provider.
                    var lastQuoteTimestamp = state.QuoteLevel >= 0
                        ? (await Cache.Blocks.GetAsync(state.QuoteLevel)).Timestamp
                        : DateTime.MinValue;

                    bool IsSameMinute(DateTime a, DateTime b)
                        => a.Year == b.Year && a.Month == b.Month && a.Day == b.Day && a.Hour == b.Hour && a.Minute == b.Minute;

                    var lastMinuteKnown = state.QuoteLevel >= 0 ? new DateTime(lastQuoteTimestamp.Year, lastQuoteTimestamp.Month, lastQuoteTimestamp.Day, lastQuoteTimestamp.Hour, lastQuoteTimestamp.Minute, 0, DateTimeKind.Utc) : DateTime.MinValue;
                    var newestMinute = new DateTime(quotes[^1].Timestamp.Year, quotes[^1].Timestamp.Month, quotes[^1].Timestamp.Day, quotes[^1].Timestamp.Hour, quotes[^1].Timestamp.Minute, 0, DateTimeKind.Utc);

                    int filled;
                    if (state.QuoteLevel >= 0 && newestMinute <= lastMinuteKnown && quotes.Count > 0)
                    {
                        var last = LastQuote(state);
                        foreach (var q in quotes)
                        {
                            q.Btc = last.Btc;
                            q.Eur = last.Eur;
                            q.Usd = last.Usd;
                            q.Cny = last.Cny;
                            q.Jpy = last.Jpy;
                            q.Krw = last.Krw;
                            q.Eth = last.Eth;
                            q.Gbp = last.Gbp;
                        }
                        filled = quotes.Count;
                    }
                    else
                    {
                        filled = await Provider.FillQuotes(quotes, LastQuote(state));
                    }
                    if (filled == 0) throw new Exception("0 quotes filled");

                    if (filled == 1)
                    {
                        SaveAndUpdate(state, quotes[0]);
                    }
                    else if (filled < 64)
                    {
                        SaveAndUpdate(state, filled == quotes.Count ? quotes : quotes.Take(filled));
                    }
                    else
                    {
                        SaveQuotes(filled == quotes.Count ? quotes : quotes.Take(filled));
                        UpdateState(state, quotes[filled - 1]);
                    }
                }
                else
                {
                    while (state.QuoteLevel < state.Level)
                    {
                        var quotes = await Db.Blocks
                            .AsNoTracking()
                            .Where(x => x.Level > state.QuoteLevel)
                            .OrderBy(x => x.Level)
                            .Take(Chunk)
                            .Select(x => new Quote
                            {
                                Level = x.Level,
                                Timestamp = x.Timestamp
                            })
                            .ToListAsync();

                        if (quotes.Count == 0) break;

                        // Reuse last values if the chunk is still within the same minute as the last saved quote
                        var lastQuoteTimestamp = state.QuoteLevel >= 0
                            ? (await Cache.Blocks.GetAsync(state.QuoteLevel)).Timestamp
                            : DateTime.MinValue;
                        var lastMinuteKnown = state.QuoteLevel >= 0 ? new DateTime(lastQuoteTimestamp.Year, lastQuoteTimestamp.Month, lastQuoteTimestamp.Day, lastQuoteTimestamp.Hour, lastQuoteTimestamp.Minute, 0, DateTimeKind.Utc) : DateTime.MinValue;
                        var newestMinute = new DateTime(quotes[^1].Timestamp.Year, quotes[^1].Timestamp.Month, quotes[^1].Timestamp.Day, quotes[^1].Timestamp.Hour, quotes[^1].Timestamp.Minute, 0, DateTimeKind.Utc);

                        int filled;
                        if (state.QuoteLevel >= 0 && newestMinute <= lastMinuteKnown && quotes.Count > 0)
                        {
                            var last = LastQuote(state);
                            foreach (var q in quotes)
                            {
                                q.Btc = last.Btc;
                                q.Eur = last.Eur;
                                q.Usd = last.Usd;
                                q.Cny = last.Cny;
                                q.Jpy = last.Jpy;
                                q.Krw = last.Krw;
                                q.Eth = last.Eth;
                                q.Gbp = last.Gbp;
                            }
                            filled = quotes.Count;
                        }
                        else
                        {
                            filled = await Provider.FillQuotes(quotes, LastQuote(state));
                        }
                        if (filled == 0) throw new Exception("0 quotes filled");

                        SaveQuotes(quotes.Count == filled ? quotes : quotes.Take(filled));
                        UpdateState(state, quotes[filled - 1]);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to commit quotes");
                if (!Config.Async) throw;
            }
        }

        public async Task Revert()
        {
            var state = Cache.AppState.Get();
            if (state.QuoteLevel >= state.Level)
            {
                try
                {
                    await Db.Database.ExecuteSqlRawAsync($@"
                        DELETE FROM ""Quotes"" WHERE ""Level"" >= {state.Level};
                        UPDATE ""AppState"" SET ""QuoteLevel"" = {state.Level - 1};");

                    state.QuoteLevel = state.Level - 1;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to revert quotes");
                    if (!Config.Async) throw;
                }
            }
        }

        void SaveQuotes(IEnumerable<Quote> quotes)
        {
            var conn = Db.Database.GetDbConnection() as NpgsqlConnection;
            if (conn.State != System.Data.ConnectionState.Open) conn.Open();
            using var writer = conn.BeginBinaryImport(@"COPY ""Quotes"" (""Level"", ""Timestamp"", ""Btc"", ""Eur"", ""Usd"", ""Cny"", ""Jpy"", ""Krw"", ""Eth"", ""Gbp"") FROM STDIN (FORMAT BINARY)");

            foreach (var q in quotes)
            {
                writer.StartRow();
                writer.Write(q.Level);
                writer.Write(q.Timestamp);
                writer.Write(q.Btc);
                writer.Write(q.Eur);
                writer.Write(q.Usd);
                writer.Write(q.Cny);
                writer.Write(q.Jpy);
                writer.Write(q.Krw);
                writer.Write(q.Eth);
                writer.Write(q.Gbp);
            }

            writer.Complete();
        }

        void UpdateState(AppState state, Quote quote)
        {
            Db.Database.ExecuteSqlRaw($@"
                UPDATE ""AppState"" SET ""QuoteLevel"" = {{0}}, ""QuoteBtc"" = {{1}}, ""QuoteEur"" = {{2}}, ""QuoteUsd"" = {{3}}, ""QuoteCny"" = {{4}}, ""QuoteJpy"" = {{5}}, ""QuoteKrw"" = {{6}}, ""QuoteEth"" = {{7}}, ""QuoteGbp"" = {{8}};",
                quote.Level, quote.Btc, quote.Eur, quote.Usd, quote.Cny, quote.Jpy, quote.Krw, quote.Eth, quote.Gbp);

            state.QuoteLevel = quote.Level;
            state.QuoteBtc = quote.Btc;
            state.QuoteEur = quote.Eur;
            state.QuoteUsd = quote.Usd;
            state.QuoteCny = quote.Cny;
            state.QuoteJpy = quote.Jpy;
            state.QuoteKrw = quote.Krw;
            state.QuoteEth = quote.Eth;
            state.QuoteGbp = quote.Gbp;
        }

        void SaveAndUpdate(AppState state, Quote quote)
        {
            Db.Database.ExecuteSqlRaw($@"
                INSERT INTO ""Quotes"" (""Level"", ""Timestamp"", ""Btc"", ""Eur"", ""Usd"", ""Cny"", ""Jpy"", ""Krw"", ""Eth"", ""Gbp"") VALUES ({{0}}, {{1}}, {{2}}, {{3}}, {{4}}, {{5}}, {{6}}, {{7}}, {{8}}, {{9}});
                UPDATE ""AppState"" SET ""QuoteLevel"" = {{0}}, ""QuoteBtc"" = {{2}}, ""QuoteEur"" = {{3}}, ""QuoteUsd"" = {{4}}, ""QuoteCny"" = {{5}}, ""QuoteJpy"" = {{6}}, ""QuoteKrw"" = {{7}}, ""QuoteEth"" = {{8}}, ""QuoteGbp"" = {{9}};",
                quote.Level, quote.Timestamp, quote.Btc, quote.Eur, quote.Usd, quote.Cny, quote.Jpy, quote.Krw, quote.Eth, quote.Gbp);

            state.QuoteLevel = quote.Level;
            state.QuoteBtc = quote.Btc;
            state.QuoteEur = quote.Eur;
            state.QuoteUsd = quote.Usd;
            state.QuoteCny = quote.Cny;
            state.QuoteJpy = quote.Jpy;
            state.QuoteKrw = quote.Krw;
            state.QuoteEth = quote.Eth;
            state.QuoteGbp = quote.Gbp;
        }

        void SaveAndUpdate(AppState state, IEnumerable<Quote> quotes)
        {
            var cnt = quotes.Count();
            var last = quotes.Last();

            var sql = new StringBuilder();
            sql.AppendLine($@"
                UPDATE ""AppState"" SET ""QuoteLevel"" = {last.Level}, ""QuoteBtc"" = {{0}}, ""QuoteEur"" = {{1}}, ""QuoteUsd"" = {{2}}, ""QuoteCny"" = {{3}}, ""QuoteJpy"" = {{4}}, ""QuoteKrw"" = {{5}}, ""QuoteEth"" = {{6}}, ""QuoteGbp"" = {{7}};
                INSERT INTO ""Quotes"" (""Level"", ""Timestamp"", ""Btc"", ""Eur"", ""Usd"", ""Cny"", ""Jpy"", ""Krw"", ""Eth"", ""Gbp"") VALUES");

            var param = new List<object>(cnt * 9 + 8)
            {
                last.Btc,
                last.Eur,
                last.Usd,
                last.Cny,
                last.Jpy,
                last.Krw,
                last.Eth,
                last.Gbp
            };

            var p = 8;
            var i = 0;

            foreach (var q in quotes)
            {
                sql.Append($"({q.Level}, {{{p++}}}, {{{p++}}}, {{{p++}}}, {{{p++}}}, {{{p++}}}, {{{p++}}}, {{{p++}}}, {{{p++}}}, {{{p++}}})");
                if (++i < cnt) sql.AppendLine(",");
                else sql.AppendLine(";");

                param.Add(q.Timestamp);
                param.Add(q.Btc);
                param.Add(q.Eur);
                param.Add(q.Usd);
                param.Add(q.Cny);
                param.Add(q.Jpy);
                param.Add(q.Krw);
                param.Add(q.Eth);
                param.Add(q.Gbp);
            }

            Db.Database.ExecuteSqlRaw(sql.ToString(), param);

            state.QuoteLevel = last.Level;
            state.QuoteBtc = last.Btc;
            state.QuoteEur = last.Eur;
            state.QuoteUsd = last.Usd;
            state.QuoteCny = last.Cny;
            state.QuoteJpy = last.Jpy;
            state.QuoteKrw = last.Krw;
            state.QuoteEth = last.Eth;
            state.QuoteGbp = last.Gbp;
        }

        IQuote LastQuote(AppState state) => state.QuoteLevel == -1 ? null : new Quote
        {
            Btc = state.QuoteBtc,
            Eur = state.QuoteEur,
            Usd = state.QuoteUsd,
            Cny = state.QuoteCny,
            Jpy = state.QuoteJpy,
            Krw = state.QuoteKrw,
            Eth = state.QuoteEth,
            Gbp = state.QuoteGbp
        };
    }

    public static class QuotesServiceExt
    {
        public static void AddQuotes(this IServiceCollection services, IConfiguration config)
        {
            if (config["Quotes:Provider:Name"] == MavrykExternalDataProvider.ProviderName)
                services.AddSingleton<IQuoteProvider, MavrykExternalDataProvider>();
            else if (config["Quotes:Provider:Name"] == CoingeckoProvider.ProviderName)
                services.AddSingleton<IQuoteProvider, CoingeckoProvider>();
            else
                services.AddSingleton<IQuoteProvider, DefaultQuotesProvider>();

            services.AddScoped<QuotesService>();
        }
    }
}
