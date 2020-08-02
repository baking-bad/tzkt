using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Dapper;

namespace Tzkt.Api.Services.Cache
{
    public class QuotesCache : DbConnection
    {
        readonly List<double>[] Quotes;
        readonly ILogger Logger;

        public QuotesCache(IConfiguration config, ILogger<QuotesCache> logger) : base(config)
        {
            Quotes = new List<double>[3];
            Logger = logger;

            Logger.LogDebug("Initializing quotes cache...");

            var sql = @"
                SELECT    ""Btc"", ""Eur"", ""Usd""
                FROM      ""Quotes""
                ORDER BY  ""Level""";

            using var db = GetConnection();
            var rows = db.Query(sql);

            Quotes[0] = rows.Select(x => (double)x.Btc).ToList();
            Quotes[1] = rows.Select(x => (double)x.Eur).ToList();
            Quotes[2] = rows.Select(x => (double)x.Usd).ToList();

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

        public async Task UpdateAsync()
        {
            var sql = $@"
                SELECT    ""Btc"", ""Eur"", ""Usd""
                FROM      ""Quotes""
                WHERE     ""Level"" >= {Quotes[0].Count}
                ORDER BY  ""Level""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql);

            Quotes[0].AddRange(rows.Select(x => (double)x.Btc));
            Quotes[1].AddRange(rows.Select(x => (double)x.Eur));
            Quotes[2].AddRange(rows.Select(x => (double)x.Usd));
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
