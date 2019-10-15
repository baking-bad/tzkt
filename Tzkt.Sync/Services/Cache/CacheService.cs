using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Tzkt.Data;
using Tzkt.Data.Models;
using Tzkt.Sync.Services.Cache;

namespace Tzkt.Sync.Services
{
    public class CacheService
    {
        readonly TzktContext Db;

        public CacheService(TzktContext db)
        {
            Db = db;
        }

        #region accounts
        public void AddAccount(Account account)
        {
            AppCache.AddAccount(account);
        }

        public async Task<bool> AccountExistsAsync(string address, AccountType? type = null)
        {
            if (String.IsNullOrEmpty(address))
                return false;

            var account = await AppCache.GetOrSetAccount(address, async () => type switch
            {
                AccountType.User => await Db.Users.FirstOrDefaultAsync(x => x.Address == address),
                AccountType.Delegate => await Db.Delegates.FirstOrDefaultAsync(x => x.Address == address),
                AccountType.Contract => await Db.Contracts.FirstOrDefaultAsync(x => x.Address == address),
                _ => await Db.Accounts.FirstOrDefaultAsync(x => x.Address == address)
            });

            return account?.Type == type || type == null;
        }

        public async Task<Account> GetAccountAsync(int? id)
        {
            if (id == null) return null;

            return await AppCache.GetOrSetAccount(id, () =>
                Db.Accounts.FirstOrDefaultAsync(x => x.Id == id)
                    ?? throw new Exception($"Account #{id} doesn't exist"));
        }

        public async Task<Account> GetAccountAsync(string address)
        {
            if (String.IsNullOrEmpty(address))
                return null;

            return await AppCache.GetOrSetAccount(address, async () =>
                await Db.Accounts.FirstOrDefaultAsync(x => x.Address == address)
                    ?? (address[0] != 't'
                        ? throw new Exception($"Contract {address} doesn't exist")
                        : new User
                        {
                            Address = address,
                            Counter = (await GetAppStateAsync()).Counter,
                            Type = AccountType.User
                        }));
        }

        public Task<Account> GetAccountAsync(Account account)
        {
            return AppCache.GetOrSetAccount(account.Address, () => Task.FromResult(account));
        }

        public void RemoveAccounts(IEnumerable<Account> accounts)
        {
            foreach (var account in accounts)
                AppCache.RemoveAccount(account);
        }

        public void RemoveAccount(Account account)
        {
            AppCache.RemoveAccount(account);
        }
        #endregion

        #region state
        public Task<AppState> GetAppStateAsync()
        {
            return AppCache.GetOrSetAppState(() =>
                Db.AppState.FirstOrDefaultAsync() ?? throw new Exception("Failed to get app state"));
        }

        public async Task<Block> GetCurrentBlockAsync()
        {
            var state = await GetAppStateAsync();
            return await AppCache.GetOrSetCurrentBlock(() =>
                Db.Blocks.FirstOrDefaultAsync(x => x.Level == state.Level));
        }

        public async Task<Block> GetPreviousBlockAsync()
        {
            var state = await GetAppStateAsync();
            return await AppCache.GetOrSetPreviousBlock(() =>
                Db.Blocks.FirstOrDefaultAsync(x => x.Level == state.Level - 1));
        }

        public Task PushBlock(Block block)
        {
            AppCache.PushBlock(block);
            return Task.CompletedTask;
        }

        public async Task PopBlock()
        {
            var prevBlock = await GetPreviousBlockAsync();
            AppCache.PushBlock(prevBlock, null);
        }
        #endregion

        #region voting
        public Task<VotingEpoch> GetCurrentEpochAsync()
        {
            return AppCache.GetOrSetVotingEpoch(() =>
                Db.VotingEpoches.Include(x => x.Periods).OrderByDescending(x => x.Level).FirstOrDefaultAsync()
                    ?? throw new Exception("Failed to get voting epoch"));
        }

        public Task AddVotingEpoch(VotingEpoch epoch)
        {
            AppCache.SetVotingEpoch(epoch);
            return Task.CompletedTask;
        }
        #endregion

        #region protocols
        public async Task<Protocol> GetCurrentProtocolAsync()
        {
            var block = await GetCurrentBlockAsync();
            return await AppCache.GetOrSetProtocol(block.ProtoCode, () =>
                Db.Protocols.FirstOrDefaultAsync(x => x.Code == block.ProtoCode)
                        ?? throw new Exception($"Proto{block.ProtoCode} doesn't exist"));
        }

        public async Task<Protocol> GetProtocolAsync(int code)
        {
            return await AppCache.GetOrSetProtocol(code, () =>
                Db.Protocols.FirstOrDefaultAsync(x => x.Code == code)
                        ?? throw new Exception($"Proto{code} doesn't exist"));
        }

        public async Task<Protocol> GetProtocolAsync(string hash)
        {
            return await AppCache.GetOrSetProtocol(hash, async () =>
                await Db.Protocols.FirstOrDefaultAsync(x => x.Hash == hash)
                        ?? new Protocol { Hash = hash, Code = await Db.Protocols.CountAsync() - 1 });
        }

        public void RemoveProtocol(Protocol protocol)
        {
            AppCache.RemoveProtocol(protocol);
        }
        #endregion

        public void Clear() => AppCache.Clear();
    }

    public static class CacheServiceExt
    {
        public static void AddCaches(this IServiceCollection services)
        {
            services.AddScoped<CacheService>();
        }
    }
}
