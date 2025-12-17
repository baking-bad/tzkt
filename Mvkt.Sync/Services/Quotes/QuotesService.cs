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
        const int Chunk = 10_000;
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
            Logger.LogInformation("Quote provider: {ProviderName} ({Mode})", Provider.GetType().Name, Config.Async ? "Async" : "Sync");

            var state = Cache.AppState.Get();
            if (state.QuoteLevel < state.Level)
            {
                var totalMissed = state.Level - state.QuoteLevel;
                Logger.LogInformation("{TotalMissed} quotes missed. QuotesSyncService will sync them in background.", totalMissed);
            }
        }

        public async Task<int> SyncBatch()
        {
            try
            {
                var state = Cache.AppState.Get();
                if (state.QuoteLevel >= state.Level)
                    return 0;

                var quotes = await LoadQuotes(state);
                if (quotes.Count == 0)
                    return 0;

                using var scope = Logger.BeginScope(new
                {
                    FromLevel = quotes.First().Level,
                    ToLevel = quotes.Last().Level
                });

                var filled = await FillQuotes(state, quotes);
                if (filled == 0)
                    return 0;

                await SaveQuotes(state, quotes, filled);
                return filled;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to sync quotes batch");
                return 0;
            }
        }

        async Task<List<Quote>> LoadQuotes(AppState state)
        {
            var remaining = state.Level - state.QuoteLevel;
            var batchSize = Math.Min(remaining, Chunk);
            
            Logger.LogDebug("Loading quotes batch: {BatchSize} quotes (remaining: {Remaining})", batchSize, remaining);
            
            return await Db.Blocks
                .AsNoTracking()
                .Where(x => x.Level > state.QuoteLevel)
                .OrderBy(x => x.Level)
                .Take(batchSize)
                .Select(x => new Quote
                {
                    Level = x.Level,
                    Timestamp = x.Timestamp
                })
                .ToListAsync();
        }

        async Task<int> FillQuotes(AppState state, List<Quote> quotes)
        {
            Logger.LogDebug("Filling quotes for levels {FirstLevel} to {LastLevel}", quotes.First().Level, quotes.Last().Level);
            var filled = await Provider.FillQuotes(quotes, LastQuote(state));
            Logger.LogDebug("Filled {Filled} out of {Total} quotes", filled, quotes.Count);
            
            if (filled == 0)
                Logger.LogWarning("Failed to fill quotes, will retry in next batch");
            
            return filled;
        }

        async Task SaveQuotes(AppState state, List<Quote> quotes, int filled)
        {
            using var tx = await Db.Database.BeginTransactionAsync();
            try
            {
                Logger.LogDebug("Saving {Filled} quotes to database", filled);
                var quotesToSave = filled == quotes.Count ? quotes : quotes.Take(filled);
                var lastQuote = quotes[filled - 1];
                
                if (filled < Config.CopyThreshold)
                    SaveQuotesWithInsert(quotesToSave);
                else
                    await SaveQuotesWithCopy(quotesToSave);

                UpdateAppState(state, lastQuote);

                await tx.CommitAsync();
                
                var newRemaining = state.Level - state.QuoteLevel;
                Logger.LogInformation("{Filled} quotes added (remaining: {Remaining})", filled, newRemaining);
            }
            catch (Exception ex) when (ex is Npgsql.PostgresException pgEx && pgEx.SqlState == "23505") // duplicate key
            {
                await tx.RollbackAsync();
                Logger.LogWarning("Duplicate key error: quotes for levels {FirstLevel}-{LastLevel} already exist. Updating state and skipping.", quotes.First().Level, quotes[filled - 1].Level);
                
                UpdateAppState(state, quotes[filled - 1]);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task Revert()
        {
            var state = Cache.AppState.Get();
            if (state.QuoteLevel >= state.Level)
            {
                try
                {
                    await Db.Database.ExecuteSqlRawAsync(@"
                        DELETE FROM ""Quotes"" WHERE ""Level"" >= {0};
                        UPDATE ""AppState"" SET ""QuoteLevel"" = {1};",
                        state.Level, state.Level - 1);

                    state.QuoteLevel = state.Level - 1;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to revert quotes");
                    if (!Config.Async) throw;
                }
            }
        }

        async Task SaveQuotesWithCopy(IEnumerable<Quote> quotes)
        {
            var conn = Db.Database.GetDbConnection() as NpgsqlConnection;
            if (conn.State != System.Data.ConnectionState.Open) conn.Open();
            
            var quotesList = quotes.ToList();
            
            // COPY quotes - heavy synchronous operation, run on thread pool to avoid blocking
            await Task.Run(() =>
            {
                using (var writer = conn.BeginBinaryImport(@"COPY ""Quotes"" (""Level"", ""Timestamp"", ""Btc"", ""Eur"", ""Usd"", ""Cny"", ""Jpy"", ""Krw"", ""Eth"", ""Gbp"") FROM STDIN (FORMAT BINARY)"))
                {
                    foreach (var q in quotesList)
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
            });
        }

        void SaveQuotesWithInsert(IEnumerable<Quote> quotes)
        {
            var quotesList = quotes.ToList();
            var cnt = quotesList.Count;

            // Pre-allocate capacity: ~100 chars for INSERT + ~100 chars per row
            var sql = new StringBuilder(capacity: 100 + cnt * 128);
            sql.Append(@"INSERT INTO ""Quotes"" (""Level"", ""Timestamp"", ""Btc"", ""Eur"", ""Usd"", ""Cny"", ""Jpy"", ""Krw"", ""Eth"", ""Gbp"") VALUES ");

            var param = new List<object>(cnt * 10);

            var p = 0;
            var i = 0;

            foreach (var q in quotesList)
            {
                sql.Append($"({{{p++}}}, {{{p++}}}, {{{p++}}}, {{{p++}}}, {{{p++}}}, {{{p++}}}, {{{p++}}}, {{{p++}}}, {{{p++}}}, {{{p++}}})");
                if (++i < cnt)
                    sql.Append(",");
                else
                    sql.Append(";");

                param.Add(q.Level);
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
        }

        void UpdateAppState(AppState state, Quote last)
        {
            Db.Database.ExecuteSqlRaw(@"
                UPDATE ""AppState""
                SET ""QuoteLevel"" = {0},
                    ""QuoteBtc"" = {1},
                    ""QuoteEur"" = {2},
                    ""QuoteUsd"" = {3},
                    ""QuoteCny"" = {4},
                    ""QuoteJpy"" = {5},
                    ""QuoteKrw"" = {6},
                    ""QuoteEth"" = {7},
                    ""QuoteGbp"" = {8};",
                last.Level, last.Btc, last.Eur, last.Usd, last.Cny,
                last.Jpy, last.Krw, last.Eth, last.Gbp);

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

        IQuote? LastQuote(AppState state)
        {
            if (state.QuoteLevel < 0)
                return null;

            return new Quote
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

