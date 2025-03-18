using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Services.Cache
{
    public class AccountsCache(CacheService cache, TzktContext db)
    {
        #region static
        static int SoftCap = 0;
        static int TargetCap = 0;
        static Dictionary<int, Account> CachedById = [];
        static Dictionary<string, Account> CachedByAddress = [];

        public static void Configure(CacheSize? size)
        {
            SoftCap = size?.SoftCap ?? 120_000;
            TargetCap = size?.TargetCap ?? 100_000;
            CachedById = new(SoftCap + 4096);
            CachedByAddress = new(SoftCap + 4096);
        }
        #endregion

        readonly CacheService Cache = cache;
        readonly TzktContext Db = db;

        public async Task ResetAsync()
        {
            CachedById.Clear();
            CachedByAddress.Clear();

            var delegates = await Db.Delegates
                .AsNoTracking()
                .ToListAsync();

            foreach (var delegat in delegates)
                Add(delegat);
        }

        public void Trim()
        {
            if (CachedById.Count > SoftCap)
            {
                var toRemove = CachedById.Values
                    .Where(x => x.Type != AccountType.Delegate)
                    .OrderBy(x => x.LastLevel)
                    .Take(CachedById.Count - TargetCap)
                    .ToList();

                foreach (var item in toRemove)
                    Remove(item);
            }
        }

        public void Add(Account account)
        {
            CachedById[account.Id] = account;
            CachedByAddress[account.Address] = account;
        }

        public void Remove(Account account)
        {
            CachedById.Remove(account.Id);
            CachedByAddress.Remove(account.Address);
        }

        public async Task Preload(IEnumerable<int> ids)
        {
            var missed = ids.Where(x => !CachedById.ContainsKey(x)).ToHashSet();
            if (missed.Count != 0)
            {
                var accounts = await Db.Accounts
                    .Where(x => missed.Contains(x.Id))
                    .ToListAsync();
                
                foreach (var account in accounts)
                    Add(account);
            }
        }

        public async Task Preload(IEnumerable<string> addresses)
        {
            var missed = addresses.Where(x => !CachedByAddress.ContainsKey(x)).ToHashSet();
            if (missed.Count != 0)
            {
                var accounts = await Db.Accounts
                    .Where(x => missed.Contains(x.Address))
                    .ToListAsync();

                foreach (var account in accounts)
                    Add(account);
            }
        }

        public async Task LoadAsync(IEnumerable<string> addresses)
        {
            var missed = addresses.Where(x => !CachedByAddress.ContainsKey(x)).ToHashSet();
            if (missed.Count != 0)
            {
                var accounts = await Db.Accounts
                    .Where(x => missed.Contains(x.Address))
                    .ToListAsync();

                foreach (var account in accounts)
                    Add(account);

                if (accounts.Count != missed.Count)
                {
                    foreach (var address in missed.Where(x => !CachedByAddress.ContainsKey(x) && x[0] == 't' && x[1] == 'z'))
                    {
                        var user = CreateUser(address);
                        Add(user);
                    }
                }
            }
        }

        public async Task<bool> ExistsAsync(string address, AccountType? type = null)
        {
            if (!CachedByAddress.TryGetValue(address, out var account))
            {
                account = await Db.Accounts.FirstOrDefaultAsync(x => x.Address == address);
                if (account != null) Add(account);
            }

            return account != null && (type == null || account.Type == type);
        }

        public Account GetCached(int id)
        {
            return CachedById[id];
        }

        public bool TryGetCached(string address, [NotNullWhen(true)] out Account? account)
        {
            return CachedByAddress.TryGetValue(address, out account);
        }

        public async Task<Account> GetAsync(int id)
        {
            if (!CachedById.TryGetValue(id, out var account))
            {
                account = await Db.Accounts.FirstOrDefaultAsync(x => x.Id == id)
                    ?? throw new Exception($"Account #{id} doesn't exist");

                Add(account);
            }

            return account;
        }

        public async Task<Account?> GetAsync(int? id)
        {
            if (id is not int _id) return null;

            if (!CachedById.TryGetValue(_id, out var account))
            {
                account = await Db.Accounts.FirstOrDefaultAsync(x => x.Id == _id)
                    ?? throw new Exception($"Account #{_id} doesn't exist");

                Add(account);
            }

            return account;
        }

        public async Task<Account> GetExistingAsync(string address)
        {
            if (!CachedByAddress.TryGetValue(address, out var account))
            {
                account = await Db.Accounts
                    .FromSqlRaw("""
                        SELECT *
                        FROM "Accounts"
                        WHERE "Address" = @p0::varchar(37)
                        """, address)
                    .FirstOrDefaultAsync()
                    ?? throw new Exception($"Account {address} doesn't exist");

                Add(account);
            }

            if (account.Type == AccountType.User && account.Balance == 0)
            {
                Db.TryAttach(account);
                account.Counter = Cache.AppState.GetManagerCounter();
            }

            return account;
        }

        public async Task<Account?> GetAsync(string? address)
        {
            if (address is null) return null;

            if (!CachedByAddress.TryGetValue(address, out var account))
            {
                account = await Db.Accounts
                    .FromSqlRaw("""
                        SELECT *
                        FROM "Accounts"
                        WHERE "Address" = @p0::varchar(37)
                        """, address)
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

        public async Task<int?> GetIdOrDefaultAsync(string address)
        {
            if (!CachedByAddress.TryGetValue(address, out var account))
            {
                account = await Db.Accounts
                    .FromSqlRaw("""
                        SELECT *
                        FROM "Accounts"
                        WHERE "Address" = @p0::varchar(37)
                        """, address)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();
            }

            return account?.Id;
        }

        public async Task<SmartRollup?> GetSmartRollupOrDefaultAsync(string address)
        {
            if (!CachedByAddress.TryGetValue(address, out var account))
            {
                account = await Db.Accounts
                    .FromSqlRaw("""
                        SELECT *
                        FROM "Accounts"
                        WHERE "Address" = @p0::varchar(37)
                        """, address)
                    .FirstOrDefaultAsync();

                if (account != null) Add(account);
            }

            return account as SmartRollup;
        }

        public bool DelegateExists(int id)
        {
            return CachedById.TryGetValue(id, out var account) && account.Type == AccountType.Delegate;
        }

        public bool DelegateExists(string address)
        {
            return CachedByAddress.TryGetValue(address, out var account) && account.Type == AccountType.Delegate;
        }

        public Data.Models.Delegate? GetDelegate(int? id)
        {
            if (id is not int _id) return null;

            if (CachedById.TryGetValue(_id, out var account) && account is Data.Models.Delegate delegat)
                return delegat;

            throw new Exception($"Unknown delegate #{id}");
        }

        public Data.Models.Delegate GetDelegate(int id)
        {
            if (CachedById.TryGetValue(id, out var account) && account is Data.Models.Delegate delegat)
                return delegat;

            throw new Exception($"Unknown delegate #{id}");
        }

        public Data.Models.Delegate? GetDelegate(string? address)
        {
            if (address is null) return null;

            if (CachedByAddress.TryGetValue(address, out var account) && account is Data.Models.Delegate delegat)
                return delegat;

            throw new Exception($"Unknown delegate '{address}'");
        }

        public Data.Models.Delegate GetExistingDelegate(string address)
        {
            if (CachedByAddress.TryGetValue(address, out var account) && account is Data.Models.Delegate delegat)
                return delegat;

            throw new Exception($"Unknown delegate '{address}'");
        }

        public Data.Models.Delegate? GetDelegateOrDefault(int? id)
        {
            if (id is not int _id) return null;

            if (CachedById.TryGetValue(_id, out var account) && account is Data.Models.Delegate delegat)
                return delegat;

            return null;
        }

        public Data.Models.Delegate? GetDelegateOrDefault(string? address)
        {
            if (address is null) return null;

            if (CachedByAddress.TryGetValue(address, out var account) && account is Data.Models.Delegate delegat)
                return delegat;

            return null;
        }

        public IEnumerable<Data.Models.Delegate> GetDelegates()
        {
            return CachedById.Values
                .Where(x => x.Type == AccountType.Delegate)
                .Select(x => (x as Data.Models.Delegate)!);
        }

        User CreateUser(string address)
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
    }
}
