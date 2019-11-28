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

        readonly Dictionary<int, RawAccount> AccountsById;
        readonly Dictionary<string, RawAccount> AccountsByAddress;
        readonly CacheConfig Config;
        readonly ILogger Logger;

        public AccountsCache(IConfiguration config, ILogger<AccountsCache> logger) : base(config)
        {
            Config = config.GetCacheConfig();
            Logger = logger;

            Logger.LogDebug("Initializing accounts cache...");

            using var db = GetConnection();
            var totalAccounts = db.QueryFirst<int>(@"SELECT COUNT(*) FROM ""Accounts""");

            var capacity = totalAccounts <= Config.MaxAccounts
                ? (int)(totalAccounts * 1.1)
                : Config.MaxAccounts;

            var sql = @"
                SELECT   *
                FROM     ""Accounts""
                ORDER BY ""LastLevel"" DESC
                LIMIT    @limit";

            using var reader = db.ExecuteReader(sql, new { limit = (int)(capacity * Config.LoadRate) });

            var userParser = reader.GetRowParser<RawUser>();
            var delegateParser = reader.GetRowParser<RawDelegate>();
            var contractParser = reader.GetRowParser<RawContract>();

            AccountsById = new Dictionary<int, RawAccount>(capacity);
            AccountsByAddress = new Dictionary<string, RawAccount>(capacity);

            while (reader.Read())
            {
                RawAccount account = reader.GetInt32(2) switch
                {
                    0 => userParser(reader),
                    1 => delegateParser(reader),
                    2 => contractParser(reader),
                    _ => throw new Exception($"Invalid raw account type")
                };

                AccountsById.Add(account.Id, account);
                AccountsByAddress.Add(account.Address, account);
            }

            Logger.LogDebug($"Loaded {AccountsByAddress.Count} of {totalAccounts} accounts");
        }

        public async Task<RawAccount> Get(int id)
        {
            if (!AccountsById.TryGetValue(id, out var account))
            {
                account = await GetRawAccount(id);
                await AddAccount(account);
            }

            return account;
        }

        public async Task<RawAccount> Get(string address)
        {
            if (!AccountsByAddress.TryGetValue(address, out var account))
            {
                account = await GetRawAccount(address);
                await AddAccount(account);
            }

            return account;
        }

        public async Task<List<(int Id, string Address)>> Update(int fromLevel)
        {
            var sql = @"
                SELECT   *
                FROM     ""Accounts""
                WHERE    ""LastLevel"" >= @fromLevel";

            using var db = GetConnection();
            using var reader = await db.ExecuteReaderAsync(sql, new { fromLevel });
            
            var userParser = reader.GetRowParser<RawUser>();
            var delegateParser = reader.GetRowParser<RawDelegate>();
            var contractParser = reader.GetRowParser<RawContract>();

            var accounts = new List<(int Id, string Address)>(64);

            await Sema.WaitAsync();

            while (reader.Read())
            {
                RawAccount account = reader.GetInt32(2) switch
                {
                    0 => userParser(reader),
                    1 => delegateParser(reader),
                    2 => contractParser(reader),
                    _ => throw new Exception($"Invalid raw account type")
                };

                AccountsById[account.Id] = account;
                AccountsByAddress[account.Address] = account;

                accounts.Add((account.Id, account.Address));
            }

            Logger.LogDebug($"Updated {accounts.Count} accounts");

            Sema.Release();

            await CheckCacheSize();

            return accounts;
        }

        Task<RawAccount> GetRawAccount(int id)
        {
            var sql = @"
                SELECT  *
                FROM    ""Accounts""
                WHERE   ""Id"" = @id
                LIMIT   1";

            return GetRawAccount(sql, new { id });
        }

        Task<RawAccount> GetRawAccount(string address)
        {
            var sql = @"
                SELECT  *
                FROM    ""Accounts""
                WHERE   ""Address"" = @address::character(36)
                LIMIT   1";

            return GetRawAccount(sql, new { address });
        }

        async Task<RawAccount> GetRawAccount(string sql, object param)
        {
            using var db = GetConnection();
            using var reader = await db.ExecuteReaderAsync(sql, param);

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
            if (AccountsByAddress.Count >= Config.MaxAccounts)
            {
                await Sema.WaitAsync();

                if (AccountsByAddress.Count >= Config.MaxAccounts)
                {
                    Logger.LogDebug($"Clearing accounts cache [{AccountsByAddress.Count}/{Config.MaxAccounts}]...");

                    #region clear addresses
                    var toRemoveByAddress = AccountsByAddress.Keys
                        .Take((int)(Config.MaxAccounts * (1 - Config.LoadRate)))
                        .ToList();

                    foreach (var key in toRemoveByAddress)
                        AccountsByAddress.Remove(key);
                    #endregion

                    #region clear ids
                    var toRemoveById = AccountsById.Keys
                        .Take((int)(Config.MaxAccounts * (1 - Config.LoadRate)))
                        .ToList();

                    foreach (var key in toRemoveById)
                        AccountsById.Remove(key);
                    #endregion

                    Logger.LogInformation($"Accounts cache cleared [{AccountsByAddress.Count}/{Config.MaxAccounts}]");
                }

                Sema.Release();
            }
        }

        async Task AddAccount(RawAccount account)
        {
            await CheckCacheSize();

            AccountsById[account.Id] = account;
            AccountsByAddress[account.Address] = account;

            Logger.LogDebug($"Account {account.Address} cached [{AccountsByAddress.Count}/{Config.MaxAccounts}]");
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
