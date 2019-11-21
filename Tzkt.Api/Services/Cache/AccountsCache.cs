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
    public class AccountsCache : DbConnection
    {
        readonly static SemaphoreSlim Sema = new SemaphoreSlim(1, 1);

        readonly Dictionary<string, RawAccount> CachedAccounts;
        readonly CacheConfig Config;
        readonly ILogger Logger;

        public AccountsCache(IConfiguration config, ILogger<AccountsCache> logger) : base(config)
        {
            Config = config.GetCacheConfig();
            Logger = logger;

            Logger.LogDebug("Initializing accounts cache...");

            var totalAccounts = GetTotalCount();
            CachedAccounts = InitCache(totalAccounts <= Config.MaxAccounts
                ? (int)(totalAccounts * 1.1)
                : Config.MaxAccounts);

            Logger.LogDebug($"Loaded {CachedAccounts.Count} of {totalAccounts} accounts");
        }

        public async Task<RawAccount> Get(string address)
        {
            if (!CachedAccounts.TryGetValue(address, out var account))
            {
                await CheckCacheSize();
                account = await GetRawAccount(address);
                CachedAccounts[address] = account;

                Logger.LogDebug($"Account {address} cached [{CachedAccounts.Count}/{Config.MaxAccounts}]");
            }

            return account;
        }

        public async Task<List<(int Id, string Address)>> Update(int fromLevel)
        {
            var sql = @"
                SELECT   *
                FROM     ""Accounts""
                WHERE    ""LastLevel"" > @fromLevel";

            using var db = GetConnection();
            using var reader = await db.ExecuteReaderAsync(sql, new { fromLevel });
            
            var userParser = reader.GetRowParser<RawUser>();
            var delegateParser = reader.GetRowParser<RawDelegate>();
            var contractParser = reader.GetRowParser<RawContract>();

            var accounts = new List<(int Id, string Address)>(reader.RecordsAffected);

            await Sema.WaitAsync();

            while (reader.Read())
            {
                var address = reader.GetString(1);
                RawAccount account = reader.GetInt32(2) switch
                {
                    0 => userParser(reader),
                    1 => delegateParser(reader),
                    2 => contractParser(reader),
                    _ => throw new Exception($"Invalid raw account type")
                };

                accounts.Add((account.Id, address));

                if (CachedAccounts.ContainsKey(address))
                    CachedAccounts[address] = account;
            }

            Sema.Release();

            return accounts;
        }

        int GetTotalCount()
        {
            using var db = GetConnection();
            return db.QueryFirst<int>(@"SELECT COUNT(*) FROM ""Accounts""");
        }

        Dictionary<string, RawAccount> InitCache(int capacity)
        {
            var sql = @"
                SELECT   *
                FROM     ""Accounts""
                ORDER BY ""LastLevel"" DESC
                LIMIT    @limit";

            using var db = GetConnection();
            using var reader = db.ExecuteReader(sql, new { limit = (int)(capacity * Config.LoadRate) });

            var userParser = reader.GetRowParser<RawUser>();
            var delegateParser = reader.GetRowParser<RawDelegate>();
            var contractParser = reader.GetRowParser<RawContract>();

            var accounts = new Dictionary<string, RawAccount>(capacity);

            while (reader.Read())
            {
                accounts.Add(reader.GetString(1), reader.GetInt32(2) switch
                {
                    0 => userParser(reader),
                    1 => delegateParser(reader),
                    2 => contractParser(reader),
                    _ => throw new Exception($"Invalid raw account type")
                });
            }

            return accounts;
        }

        async Task<RawAccount> GetRawAccount(string address)
        {
            var sql = @"
                SELECT  *
                FROM    ""Accounts""
                WHERE   ""Address"" = @address::character(36)
                LIMIT   1";

            using var db = GetConnection();
            using var reader = await db.ExecuteReaderAsync(sql, new { address });

            if (!reader.Read()) return null;

            return reader.GetInt32(2) switch
            {
                0 => reader.GetRowParser<RawUser>()(reader),
                1 => reader.GetRowParser<RawDelegate>()(reader),
                2 => reader.GetRowParser<RawContract>()(reader),
                _ => throw new Exception($"Invalid raw account type")
            };
        }

        async Task CheckCacheSize()
        {
            if (CachedAccounts.Count >= Config.MaxAccounts)
            {
                await Sema.WaitAsync();

                if (CachedAccounts.Count >= Config.MaxAccounts)
                {
                    Logger.LogDebug($"Clearing accounts cache [{CachedAccounts.Count}/{Config.MaxAccounts}]...");

                    var toRemove = CachedAccounts.Keys
                        .Take((int)(Config.MaxAccounts * (1 - Config.LoadRate)))
                        .ToList();

                    foreach (var key in toRemove)
                        CachedAccounts.Remove(key);

                    Logger.LogInformation($"Accounts cache cleared [{CachedAccounts.Count}/{Config.MaxAccounts}]");
                }

                Sema.Release();
            }
        }
    }

    public static class AccountsCacheExt
    {
        public static void AddAccountsCache(this IServiceCollection services)
        {
            services.AddSingleton<AccountsCache>();
        }
    }
}
