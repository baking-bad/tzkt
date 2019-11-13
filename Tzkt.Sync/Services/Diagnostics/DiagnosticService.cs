using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tzkt.Data;
using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;
using Tzkt.Sync.Protocols;
using Tzkt.Sync.Services.Diagnostics;

namespace Tzkt.Sync.Services
{
    public class DiagnosticService
    {
        readonly TzktContext Db;
        readonly TezosNode Node;

        public DiagnosticService(TzktContext db, TezosNode node)
        {
            Db = db;
            Node = node;
        }

        public async Task Run(int level, int operations)
        {
            if (level < 2) return;

            var entries = Db.ChangeTracker.Entries();

            if (operations > entries.Count(x => x.Entity is BaseOperation && x.State == EntityState.Added))
                throw new Exception($"Diagnostics failed: wrong operations count");

            var state = entries.FirstOrDefault(x => x.Entity is AppState).Entity;
            var proto = entries.FirstOrDefault(x => x.Entity is Protocol).Entity as Protocol;

            var accounts = entries.Where(x =>
                x.Entity is Account &&
                (x.State == EntityState.Modified ||
                x.State == EntityState.Added))
                .Select(x => x.Entity as Account);

            await TestState(level, state as AppState);

            foreach (var account in accounts)
            {
                if (account is Data.Models.Delegate delegat)
                    await TestDelegate(level, delegat, proto);
                
                await TestAccount(level, account);
            }
        }

        public async Task Run(int level)
        {
            if (level < 2) return;

            var entries = Db.ChangeTracker.Entries();

            var state = entries.FirstOrDefault(x => x.Entity is AppState).Entity;
            var proto = entries.FirstOrDefault(x => x.Entity is Protocol).Entity as Protocol;

            var accounts = entries.Where(x =>
                x.Entity is Account &&
                (x.State == EntityState.Modified ||
                x.State == EntityState.Added))
                .Select(x => x.Entity as Account);

            await TestState(level, state as AppState);

            foreach (var account in accounts)
            {
                
                if (account is Data.Models.Delegate delegat)
                    await TestDelegate(level, delegat, proto);

                await TestAccount(level, account);
            }
        }

        async Task TestState(int level, AppState state)
        {
            var globalCounter = await GetGlobalCounter(level);

            if (globalCounter != state.ManagerCounter)
                throw new Exception($"Diagnostics failed: wrong global counter");
        }

        async Task TestDelegate(int level, Data.Models.Delegate delegat, Protocol proto)
        {
            var remote = await GetRemoteDelegate(level, delegat.Address);

            if (remote.Balance != delegat.Balance)
                throw new Exception($"Diagnostics failed: wrong balance {delegat.Address}");

            if (remote.Deactivated != !delegat.Staked)
                throw new Exception($"Diagnostics failed: wrong delegate state {delegat.Address}");

            if (remote.GracePeriod != (delegat.DeactivationLevel - 2) / proto.BlocksPerCycle)
                throw new Exception($"Diagnostics failed: wrong delegate grace period {delegat.Address}");

            if (remote.Delegators.Count != delegat.Delegators && level >= 655360 && remote.Delegators.Count - delegat.Delegators != 1)
                throw new Exception($"Diagnostics failed: wrong delegators count {delegat.Address}");

            if ((remote.FrozenBalances.Count > 0 ? remote.FrozenBalances.Sum(x => x.Deposit) : 0) != delegat.FrozenDeposits)
                throw new Exception($"Diagnostics failed: wrong frozen deposits {delegat.Address}");

            if ((remote.FrozenBalances.Count > 0 ? remote.FrozenBalances.Sum(x => x.Fees) : 0) != delegat.FrozenFees)
                throw new Exception($"Diagnostics failed: wrong frozen fees {delegat.Address}");

            if ((remote.FrozenBalances.Count > 0 ? remote.FrozenBalances.Sum(x => x.Rewards) : 0) != delegat.FrozenRewards)
                throw new Exception($"Diagnostics failed: wrong frozen rewards {delegat.Address}");

            if (remote.StakingBalance != delegat.StakingBalance)
                throw new Exception($"Diagnostics failed: wrong staking balance {delegat.Address}");
        }

        async Task TestAccount(int level, Account account)
        {
            var remote = await GetRemoteContract(level, account.Address);

            if (!(account is Data.Models.Delegate) && remote.Balance != account.Balance)
                throw new Exception($"Diagnostics failed: wrong balance {account.Address}");

            if ((level < 655360 || account.Type != AccountType.Contract) && remote.Balance > 0 && remote.Counter != account.Counter)
                throw new Exception($"Diagnostics failed: wrong counter {account.Address}");

            if (!(account is Data.Models.Delegate) && remote.Delegate.Value != account.Delegate?.Address &&
                !(account is Contract c && (c.Manager == null || c.Manager.Address == remote.Delegate.Value)))
                throw new Exception($"Diagnostics failed: wrong delegate {account.Address}");
        }

        async Task<int> GetGlobalCounter(int level)
        {
            var stream = await Node.GetGlobalCounterAsync(level);
            return await JsonSerializer.DeserializeAsync<int>(stream, SerializerOptions.Default);
        }

        async Task<RemoteContract> GetRemoteContract(int level, string address)
        {
            if (level >= 655360)
                return await GetRemoteContractBaby(level, address);

            try
            {
                var stream = await Node.GetContractAsync(level, address);
                var contract = await JsonSerializer.DeserializeAsync<RemoteContract>(stream, SerializerOptions.Default);

                if (!contract.IsValidFormat())
                    throw new SerializationException($"invalid format");

                return contract;
            }
            catch (JsonException ex)
            {
                throw new SerializationException($"[{ex.Path}] {ex.Message}");
            }
        }

        async Task<RemoteContract> GetRemoteContractBaby(int level, string address)
        {
            try
            {
                var stream = await Node.GetContractAsync(level, address);
                var contract = await JsonSerializer.DeserializeAsync<RemoteContractBaby>(stream, SerializerOptions.Default);

                if (!contract.IsValidFormat())
                    throw new SerializationException($"invalid format {level} - {address}");

                return new RemoteContract
                {
                    Balance = contract.Balance,
                    Counter = contract.Counter,
                    Delegate = new RemoteContractDelegate
                    {
                        Setable = true,
                        Value = contract.Delegate
                    }
                };
            }
            catch (JsonException ex)
            {
                throw new SerializationException($"[{ex.Path}] {ex.Message}");
            }
        }

        async Task<RemoteDelegate> GetRemoteDelegate(int level, string address)
        {
            try
            {
                var stream = await Node.GetDelegateAsync(level, address);
                var delegat = await JsonSerializer.DeserializeAsync<RemoteDelegate>(stream, SerializerOptions.Default);

                if (!delegat.IsValidFormat())
                    throw new SerializationException($"invalid format {level} - {address}");

                return delegat;
            }
            catch (JsonException ex)
            {
                throw new SerializationException($"[{ex.Path}] {ex.Message}");
            }
        }
    }

    static class DiagnosticServiceExt
    {
        public static void AddDiagnostics(this IServiceCollection services)
        {
            services.AddScoped<DiagnosticService>();
        }
    }
}
