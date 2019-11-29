using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Dapper;

using Tzkt.Api.Services.Metadata;
using Tzkt.Api.Models;

namespace Tzkt.Api.Services.Cache
{
    public class AccountsCache : DbConnection
    {
        readonly static SemaphoreSlim Sema = new SemaphoreSlim(1, 1);

        readonly Dictionary<int, RawAccount> AccountsById;
        readonly Dictionary<string, RawAccount> AccountsByAddress;

        readonly AccountMetadataService Metadata;
        readonly CacheConfig Config;
        readonly ILogger Logger;

        public AccountsCache(AccountMetadataService metadata, IConfiguration config, ILogger<AccountsCache> logger) : base(config)
        {
            Metadata = metadata;
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

            await CheckCacheSizeAsync();

            return accounts;
        }

        #region metadata
        public AccountMetadata GetMetadata(int id) => Metadata[id];

        public string GetAliasName(int id) => Metadata[id]?.Alias;

        public Alias GetAlias(int id) => new Alias
        {
            Address = Get(id).Address,
            Name = Metadata[id]?.Alias
        };

        public async Task<Alias> GetAliasAsync(int id) => new Alias
        {
            Address = (await GetAsync(id)).Address,
            Name = Metadata[id]?.Alias
        };
        #endregion

        public RawAccount Get(int id)
        {
            if (!AccountsById.TryGetValue(id, out var account))
            {
                account = GetRawAccount(id);
                if (account != null) AddAccount(account);
            }

            return account;
        }

        public async Task<RawAccount> GetAsync(int id)
        {
            if (!AccountsById.TryGetValue(id, out var account))
            {
                account = await GetRawAccountAsync(id);
                if (account != null) AddAccount(account);
            }

            return account;
        }

        public RawAccount Get(string address)
        {
            if (!AccountsByAddress.TryGetValue(address, out var account))
            {
                account = GetRawAccount(address);
                if (account != null) AddAccount(account);
            }

            return account;
        }

        public async Task<RawAccount> GetAsync(string address)
        {
            if (!AccountsByAddress.TryGetValue(address, out var account))
            {
                account = await GetRawAccountAsync(address);
                if (account != null) AddAccount(account);
            }

            return account;
        }

        RawAccount GetRawAccount(int id)
        {
            var sql = @"
                SELECT  *
                FROM    ""Accounts""
                WHERE   ""Id"" = @id
                LIMIT   1";

            return GetRawAccount(sql, new { id });
        }

        Task<RawAccount> GetRawAccountAsync(int id)
        {
            var sql = @"
                SELECT  *
                FROM    ""Accounts""
                WHERE   ""Id"" = @id
                LIMIT   1";

            return GetRawAccountAsync(sql, new { id });
        }

        RawAccount GetRawAccount(string address)
        {
            var sql = @"
                SELECT  *
                FROM    ""Accounts""
                WHERE   ""Address"" = @address::character(36)
                LIMIT   1";

            return GetRawAccount(sql, new { address });
        }

        Task<RawAccount> GetRawAccountAsync(string address)
        {
            var sql = @"
                SELECT  *
                FROM    ""Accounts""
                WHERE   ""Address"" = @address::character(36)
                LIMIT   1";

            return GetRawAccountAsync(sql, new { address });
        }

        RawAccount GetRawAccount(string sql, object param)
        {
            using var db = GetConnection();
            using var reader = db.ExecuteReader(sql, param);

            if (!reader.Read()) return null;

            return reader.GetInt32(2) switch
            {
                0 => reader.GetRowParser<RawUser>()(reader),
                1 => reader.GetRowParser<RawDelegate>()(reader),
                2 => reader.GetRowParser<RawContract>()(reader),
                _ => throw new Exception($"Invalid raw account type")
            };
        }

        async Task<RawAccount> GetRawAccountAsync(string sql, object param)
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

        void AddAccount(RawAccount account)
        {
            CheckCacheSize();

            AccountsById[account.Id] = account;
            AccountsByAddress[account.Address] = account;

            Logger.LogDebug($"Account {account.Address} cached [{AccountsByAddress.Count}/{Config.MaxAccounts}]");
        }

        void CheckCacheSize()
        {
            if (AccountsByAddress.Count >= Config.MaxAccounts)
            {
                Sema.Wait();

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
    }

    public static class AccountsCacheExt
    {
        public static void AddAccountsCache(this IServiceCollection services)
        {
            services.AddSingleton<AccountsCache>();
        }
    }
}
