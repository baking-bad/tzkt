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

        public CacheService(TzktContext db) => Db = db;

        public void Clear() => AppCache.Clear();

        #region accounts
        public async Task LoadDelegates()
        {
            foreach (var delegat in await Db.Delegates.AsNoTracking().ToListAsync())
                AddAccount(delegat);
        }

        public async Task PrepareAccounts(List<string> addresses)
        {
            AppCache.EnsureAccountsCap(addresses.Count);

            var missed = addresses.Where(x => !AppCache.HasAccount(x)).ToList();
            if (missed.Count > 0)
            {
                var accounts = await Db.Accounts.Where(x => missed.Contains(x.Address)).ToListAsync();

                foreach (var account in accounts)
                    AddAccount(account);

                if (accounts.Count < missed.Count)
                {
                    foreach (var account in missed.Where(x => !AppCache.HasAccount(x) && x[0] == 't'))
                        AddAccount(new User
                        {
                            Address = account,
                            Type = AccountType.User
                        });
                }
            }
        }

        public async Task<bool> AccountExistsAsync(string address, AccountType? type = null)
        {
            if (string.IsNullOrEmpty(address))
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

        public void AddAccount(Account account)
        {
            AppCache.AddAccount(account);
        }

        public Task<Account> GetAccountAsync(Account account)
        {
            return AppCache.GetOrSetAccount(account.Address, () => Task.FromResult(account));
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
            if (string.IsNullOrEmpty(address))
                return null;

            var account = await AppCache.GetOrSetAccount(address, async () =>
                await Db.Accounts.FirstOrDefaultAsync(x => x.Address == address)
                    ?? (address[0] == 't'
                        ? new User
                        {
                            Address = address,
                            Counter = (await GetAppStateAsync()).ManagerCounter,
                            Type = AccountType.User
                        }
                        : null));

            if (account?.Type == AccountType.User && account.Balance == 0)
            {
                Db.TryAttach(account);
                account.Counter = (await GetAppStateAsync()).ManagerCounter;
            }

            return account;
        }

        public async Task<Data.Models.Delegate> GetDelegateAsync(string address)
        {
            var delegat = await AppCache.GetOrSetAccount(address, async () =>
                await Db.Delegates.FirstOrDefaultAsync(x => x.Address == address)) as Data.Models.Delegate;

            return delegat ?? throw new Exception($"unknown delegate '{address}'");
        }

        public async Task<Data.Models.Delegate> GetDelegateOrDefaultAsync(string address)
        {
            if (string.IsNullOrEmpty(address))
                return null;

            return await AppCache.GetOrSetAccount(address, async () =>
                await Db.Delegates.FirstOrDefaultAsync(x => x.Address == address)) as Data.Models.Delegate;
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

        public async Task<int> NextCounterAsync()
        {
            var state = await GetAppStateAsync();

            Db.TryAttach(state);
            return ++state.GlobalCounter;
        }

        public async Task IncreaseManagerCounter(int value)
        {
            var state = await GetAppStateAsync();

            Db.TryAttach(state);
            state.ManagerCounter += value;
        }

        public async Task ReleaseCounterAsync(bool manager = false)
        {
            var state = await GetAppStateAsync();

            Db.TryAttach(state);
            if (manager) --state.ManagerCounter;
            //--state.GlobalCounter;
        }
        #endregion

        #region blocks
        public void AddBlock(Block block)
        {
            AppCache.AddBlock(block);
        }

        public async Task<Block> GetCurrentBlockAsync()
        {
            var state = await GetAppStateAsync();
            return await GetBlockAsync(state.Level);
        }

        public async Task<Block> GetPreviousBlockAsync()
        {
            var state = await GetAppStateAsync();
            return await GetBlockAsync(state.Level - 1);
        }

        public async Task<Block> GetBlockAsync(int level)
        {
            return await AppCache.GetOrSetBlock(level, async () =>
                await Db.Blocks.FirstOrDefaultAsync(x => x.Level == level)
                    ?? throw new Exception($"Block #{level} doesn't exist"));
        }

        public void RemoveBlock(Block block)
        {
            AppCache.RemoveBlock(block);
        }
        #endregion

        #region voting
        public void AddVotingPeriod(VotingPeriod period)
        {
            AppCache.SetVotingPeriod(period);
        }

        public Task<VotingPeriod> GetCurrentVotingPeriodAsync()
        {
            return AppCache.GetOrSetVotingPeriod(() =>
                Db.VotingPeriods.OrderByDescending(x => x.StartLevel).FirstOrDefaultAsync()
                    ?? throw new Exception("Failed to get voting epoch"));
        }

        public void RemoveVotingPeriod()
        {
            AppCache.RemoveVotingPeriod();
        }
        #endregion

        #region protocols
        public void AddProtocol(Protocol protocol)
        {
            AppCache.AddProtocol(protocol);
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
                    ?? throw new Exception($"Protocol {hash} doesn't exist"));
        }

        public void RemoveProtocol(Protocol protocol)
        {
            AppCache.RemoveProtocol(protocol);
        }
        #endregion

        #region protocols
        public void AddProposal(Proposal proposal)
        {
            AppCache.AddProposal(proposal);
        }

        public async Task<Proposal> GetProposalAsync(int id)
        {
            return await AppCache.GetOrSetProposal(id, () =>
                Db.Proposals.FirstOrDefaultAsync(x => x.Id == id)
                    ?? throw new Exception($"Proposal {id} doesn't exist"));
        }

        public async Task<Proposal> GetProposalAsync(string hash)
        {
            return await AppCache.GetOrSetProposal(hash, async () =>
                await Db.Proposals.FirstOrDefaultAsync(x => x.Hash == hash)
                    ?? throw new Exception($"Proposal {hash} doesn't exist"));
        }

        public async Task<Proposal> GetOrSetProposalAsync(string hash, Func<Task<Proposal>> createProposal)
        {
            return await AppCache.GetOrSetProposal(hash, async () =>
                await Db.Proposals.FirstOrDefaultAsync(x => x.Hash == hash)
                    ?? await createProposal());
        }

        public void RemoveProposal(Proposal proposal)
        {
            AppCache.RemoveProposal(proposal);
        }
        #endregion
    }

    public static class CacheServiceExt
    {
        public static void AddCaches(this IServiceCollection services)
        {
            services.AddScoped<CacheService>();
        }
    }
}
