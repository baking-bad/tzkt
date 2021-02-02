using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Dapper;

using Tzkt.Api.Models;
using Tzkt.Api.Services.Metadata;

namespace Tzkt.Api.Services.Cache
{
    public class AccountsCache : DbConnection
    {
        readonly Dictionary<int, RawAccount> AccountsById;
        readonly Dictionary<string, RawAccount> AccountsByAddress;
        int LastUpdate;

        readonly StateCache State;
        readonly AccountMetadataService Metadata;
        readonly CacheConfig Config;
        readonly ILogger Logger;

        public AccountsCache(StateCache state, AccountMetadataService metadata, IConfiguration config, ILogger<AccountsCache> logger) : base(config)
        {
            logger.LogDebug("Initializing accounts cache...");

            State = state;
            Metadata = metadata;
            Config = config.GetCacheConfig();
            Logger = logger;

            var limit = Config.MaxAccounts > 0
                ? (int)(Math.Min(State.Current.AccountsCount, Config.MaxAccounts) * Config.LoadRate)
                : (int)(State.Current.AccountsCount * Config.LoadRate);

            var capacity = Config.MaxAccounts > 0
                ? Math.Min((int)(limit * 1.1), Config.MaxAccounts + 1)
                : (int)(limit * 1.1);

            using var db = GetConnection();
            using var reader = db.ExecuteReader(
                @"SELECT * FROM ""Accounts"" ORDER BY ""LastLevel"" DESC LIMIT @limit", new { limit });

            AccountsById = new Dictionary<int, RawAccount>(capacity);
            AccountsByAddress = new Dictionary<string, RawAccount>(capacity);

            var parsers = new Func<IDataReader, RawAccount>[3]
            {
                reader.GetRowParser<RawUser>(),
                reader.GetRowParser<RawDelegate>(),
                reader.GetRowParser<RawContract>()
            };

            while (reader.Read())
            {
                var account = parsers[reader.GetInt32(2)](reader);
                AccountsById.Add(account.Id, account);
                AccountsByAddress.Add(account.Address, account);
            }

            LastUpdate = State.Current.Level;

            logger.LogInformation("Loaded {1} of {2} accounts", AccountsByAddress.Count, State.Current.AccountsCount);
        }

        public async Task UpdateAsync()
        {
            Logger.LogDebug("Updating accounts cache...");

            var fromLevel = Math.Min(LastUpdate, State.ValidLevel);

            #region check reorg
            if (State.Reorganized)
            {
                List<RawAccount> corrupted;
                lock (this)
                {
                    corrupted = AccountsByAddress.Values
                        .Where(x => x.LastLevel > fromLevel)
                        .ToList();

                    foreach (var account in corrupted)
                    {
                        AccountsById.Remove(account.Id);
                        AccountsByAddress.Remove(account.Address);
                    }
                }
                Logger.LogDebug("Removed {1} corrupted accounts", corrupted.Count);
            }
            #endregion

            using var db = GetConnection();
            using var reader = await db.ExecuteReaderAsync(
                @"SELECT * FROM ""Accounts"" WHERE ""LastLevel"" > @fromLevel", new { fromLevel });

            var parsers = new Func<IDataReader, RawAccount>[3]
            {
                reader.GetRowParser<RawUser>(),
                reader.GetRowParser<RawDelegate>(),
                reader.GetRowParser<RawContract>()
            };

            var cnt = 0;
            while (reader.Read())
            {
                var accType = reader.GetInt32(2);
                Add(parsers[accType](reader)); // TODO: don't cache new accounts until they are requested
                cnt++;
            }

            LastUpdate = State.Current.Level;
            Logger.LogDebug("Updated {1} accounts since block {2}", cnt, fromLevel);
        }

        #region metadata
        public AccountMetadata GetMetadata(int id) => Metadata[id];

        public Alias GetAlias(int id) => new Alias
        {
            // WARN: possible NullReferenceException if chain reorgs during request execution (very unlikely)
            Address = Get(id).Address,
            Name = Metadata[id]?.Alias
        };

        public async Task<Alias> GetAliasAsync(int id) => new Alias
        {
            // WARN: possible NullReferenceException if chain reorgs during request execution (very unlikely)
            Address = (await GetAsync(id)).Address,
            Name = Metadata[id]?.Alias
        };
        #endregion

        public RawAccount Get(int id)
        {
            if (!TryGetSafe(id, out var account))
            {
                account = LoadRawAccount(id);
                if (account != null) Add(account);
            }

            return account;
        }

        public async Task<RawAccount> GetAsync(int id)
        {
            if (!TryGetSafe(id, out var account))
            {
                account = await LoadRawAccountAsync(id);
                if (account != null) Add(account);
            }

            return account;
        }

        public RawAccount Get(string address)
        {
            if (!TryGetSafe(address, out var account))
            {
                account = LoadRawAccount(address);
                if (account != null) Add(account);
            }

            return account;
        }

        public async Task<RawAccount> GetAsync(string address)
        {
            if (!TryGetSafe(address, out var account))
            {
                account = await LoadRawAccountAsync(address);
                if (account != null) Add(account);
            }

            return account;
        }

        RawAccount LoadRawAccount(int id)
        {
            var sql = @"SELECT * FROM ""Accounts"" WHERE ""Id"" = @id LIMIT 1";
            return LoadRawAccount(sql, new { id });
        }

        Task<RawAccount> LoadRawAccountAsync(int id)
        {
            var sql = @"SELECT * FROM ""Accounts"" WHERE ""Id"" = @id LIMIT 1";
            return LoadRawAccountAsync(sql, new { id });
        }

        RawAccount LoadRawAccount(string address)
        {
            var sql = @"SELECT * FROM ""Accounts"" WHERE ""Address"" = @address::character(36) LIMIT 1";
            return LoadRawAccount(sql, new { address });
        }

        Task<RawAccount> LoadRawAccountAsync(string address)
        {
            var sql = @"SELECT * FROM ""Accounts"" WHERE ""Address"" = @address::character(36) LIMIT 1";
            return LoadRawAccountAsync(sql, new { address });
        }

        RawAccount LoadRawAccount(string sql, object param)
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

        async Task<RawAccount> LoadRawAccountAsync(string sql, object param)
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

        bool TryGetSafe(int id, out RawAccount account)
        {
            lock (this)
            {
                return AccountsById.TryGetValue(id, out account);
            }
        }

        bool TryGetSafe(string address, out RawAccount account)
        {
            lock (this)
            {
                return AccountsByAddress.TryGetValue(address, out account);
            }
        }

        void Add(RawAccount account)
        {
            lock (this)
            {
                #region check limits
                if (Config.MaxAccounts > 0 && AccountsByAddress.Count >= Config.MaxAccounts)
                {
                    Logger.LogDebug("Cache is full. Clearing...");
                    var oldest = AccountsByAddress.Values
                        .Take((int)(AccountsByAddress.Count * 0.25))
                        .ToList();

                    foreach (var acc in oldest)
                    {
                        AccountsById.Remove(acc.Id);
                        AccountsByAddress.Remove(acc.Address);
                    }
                    Logger.LogDebug("Removed {1} oldest accounts", oldest.Count);
                }
                #endregion

                AccountsById[account.Id] = account;
                AccountsByAddress[account.Address] = account;
            }
            Logger.LogDebug("Account {1} cached", account.Address);
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
