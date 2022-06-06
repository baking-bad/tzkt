using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using Tzkt.Data;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Services.Cache
{
    public class AccountsCache
    {
        public const int MaxAccounts = 16 * 4096; //TODO: set limits in app settings

        static readonly Dictionary<int, Account> CachedById = new(MaxAccounts);
        static readonly Dictionary<string, Account> CachedByAddress = new(MaxAccounts);

        readonly CacheService Cache;
        readonly TzktContext Db;

        public AccountsCache(CacheService cache, TzktContext db)
        {
            Cache = cache;
            Db = db;
        }

        public async Task ResetAsync()
        {
            CachedById.Clear();
            CachedByAddress.Clear();

            var delegates = await Db.Delegates.AsNoTracking().ToListAsync();
            foreach (var delegat in delegates)
            {
                CachedById.Add(delegat.Id, delegat);
                CachedByAddress.Add(delegat.Address, delegat);
            }
        }

        public async Task Preload(IEnumerable<int> ids)
        {
            var missed = ids.Where(x => !CachedById.ContainsKey(x)).ToHashSet();
            if (missed.Count > 0)
            {
                CheckSpaceFor(missed.Count);

                var accounts = await Db.Accounts.Where(x => missed.Contains(x.Id)).ToListAsync();
                
                foreach (var account in accounts)
                {
                    CachedById[account.Id] = account;
                    CachedByAddress[account.Address] = account;
                }
            }
        }

        public async Task Preload(IEnumerable<string> addresses)
        {
            var missed = addresses.Where(x => !CachedByAddress.ContainsKey(x)).ToHashSet();
            if (missed.Count > 0)
            {
                CheckSpaceFor(missed.Count);

                var accounts = await Db.Accounts.Where(x => missed.Contains(x.Address)).ToListAsync();

                foreach (var account in accounts)
                {
                    CachedById[account.Id] = account;
                    CachedByAddress[account.Address] = account;
                }
            }
        }

        public async Task LoadAsync(IEnumerable<string> addresses)
        {
            var missed = addresses.Where(x => !CachedByAddress.ContainsKey(x)).ToHashSet();
            if (missed.Count > 0)
            {
                CheckSpaceFor(missed.Count);

                var accounts = await Db.Accounts.Where(x => missed.Contains(x.Address)).ToListAsync();

                foreach (var account in accounts)
                {
                    CachedById[account.Id] = account;
                    CachedByAddress[account.Address] = account;
                }

                if (accounts.Count < missed.Count)
                {
                    foreach (var address in missed.Where(x => !CachedByAddress.ContainsKey(x) && x[0] == 't' && x[1] == 'z'))
                    {
                        var account = CreateUser(address);
                        CachedById[account.Id] = account;
                        CachedByAddress[account.Address] = account;
                    }
                }
            }
        }

        public void Add(Account account)
        {
            CheckSpaceFor(1);
            CachedById[account.Id] = account;
            CachedByAddress[account.Address] = account;
        }

        public void Remove(IEnumerable<Account> accounts)
        {
            foreach (var account in accounts)
            {
                CachedById.Remove(account.Id);
                CachedByAddress.Remove(account.Address);
            }
        }

        public void Remove(Account account)
        {
            CachedById.Remove(account.Id);
            CachedByAddress.Remove(account.Address);
        }

        public async Task<bool> ExistsAsync(string address, AccountType? type = null)
        {
            if (string.IsNullOrEmpty(address))
                return false;

            if (!CachedByAddress.TryGetValue(address, out var account))
            {
                account = type switch
                {
                    AccountType.User => await Db.Users.FirstOrDefaultAsync(x => x.Address == address),
                    AccountType.Delegate => await Db.Delegates.FirstOrDefaultAsync(x => x.Address == address),
                    AccountType.Contract => await Db.Contracts.FirstOrDefaultAsync(x => x.Address == address),
                    AccountType.Rollup => await Db.Rollups.FirstOrDefaultAsync(x => x.Address == address),
                    _ => await Db.Accounts.FirstOrDefaultAsync(x => x.Address == address)
                };

                if (account != null) Add(account);
            }

            return account != null && (account.Type == type || type == null);
        }

        public Account GetCached(int id)
        {
            return CachedById[id];
        }

        public Account GetCached(string address)
        {
            return CachedByAddress[address];
        }

        public bool TryGetCached(string address, out Account account)
        {
            return CachedByAddress.TryGetValue(address, out account);
        }

        public async Task<Account> GetAsync(int? id)
        {
            if (id == null) return null;

            if (!CachedById.TryGetValue((int)id, out var account))
            {
                account = await Db.Accounts.FirstOrDefaultAsync(x => x.Id == id)
                    ?? throw new Exception($"Account #{id} doesn't exist");

                Add(account);
            }

            return account;
        }

        public async Task<Account> GetAsync(string address)
        {
            if (string.IsNullOrEmpty(address))
                return null;

            if (!CachedByAddress.TryGetValue(address, out var account))
            {
                account = await Db.Accounts
                    .FromSqlRaw(@"SELECT * FROM ""Accounts"" WHERE ""Address"" = @p0::varchar(37)", address)
                    .FirstOrDefaultAsync()
                    ?? (address[0] == 't' && address[1] == 'z' ? CreateUser(address) : null);

                if (account != null) Add(account);
            }

            if (account?.Type == AccountType.User && account.Balance == 0)
            {
                Db.TryAttach(account);
                account.Counter = Cache.AppState.GetManagerCounter();
            }

            return account;
        }

        public bool DelegateExists(int id)
        {
            return CachedById.TryGetValue(id, out var account) && account is Data.Models.Delegate;
        }

        public bool DelegateExists(string address)
        {
            return CachedByAddress.TryGetValue(address, out var account) && account is Data.Models.Delegate;
        }

        public Data.Models.Delegate GetDelegate(int? id)
        {
            if (id == null) return null;

            if (CachedById.TryGetValue((int)id, out var account) && account is Data.Models.Delegate delegat)
                return delegat;

            throw new Exception($"Unknown delegate #{id}");
        }

        public Data.Models.Delegate GetDelegate(string address)
        {
            if (string.IsNullOrEmpty(address))
                return null;

            if (CachedByAddress.TryGetValue(address, out var account) && account is Data.Models.Delegate delegat)
                return delegat;

            throw new Exception($"Unknown delegate '{address}'");
        }

        public Data.Models.Delegate GetDelegateOrDefault(int? id)
        {
            if (id == null) return null;

            if (CachedById.TryGetValue((int)id, out var account) && account is Data.Models.Delegate delegat)
                return delegat;

            return null;
        }

        public Data.Models.Delegate GetDelegateOrDefault(string address)
        {
            if (string.IsNullOrEmpty(address))
                return null;

            if (CachedByAddress.TryGetValue(address, out var account) && account is Data.Models.Delegate delegat)
                return delegat;

            return null;
        }

        public IEnumerable<Data.Models.Delegate> GetDelegates()
        {
            return CachedById.Values
                .Where(x => x.Type == AccountType.Delegate)
                .Select(x => (Data.Models.Delegate)x);
        }

        Account CreateUser(string address)
        {
            var account = new User
            {
                Id = Cache.AppState.NextAccountId(),
                Address = address,
                FirstLevel = Cache.AppState.GetNextLevel(),
                LastLevel = Cache.AppState.GetNextLevel(),
                Type = AccountType.User
            };

            Db.Accounts.Add(account);
            return account;
        }

        void CheckSpaceFor(int count)
        {
            if (CachedById.Count + count > MaxAccounts)
            {
                var oldest = CachedById.Values
                    .Where(x => x.Type != AccountType.Delegate)
                    .OrderBy(x => x.LastLevel)
                    .TakeLast(MaxAccounts / 4);

                foreach (var id in oldest.Select(x => x.Id).ToList())
                    CachedById.Remove(id);

                foreach (var address in oldest.Select(x => x.Address).ToList())
                    CachedByAddress.Remove(address);
            }
        }
    }
}
