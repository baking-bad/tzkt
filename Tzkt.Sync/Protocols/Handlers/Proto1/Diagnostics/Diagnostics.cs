using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using Tzkt.Data;
using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;
using Tzkt.Sync.Services;

namespace Tzkt.Sync.Protocols.Proto1
{
    class Diagnostics : IDiagnostics
    {
        protected readonly TzktContext Db;
        protected readonly CacheService Cache;
        protected readonly IRpc Rpc;

        public Diagnostics(ProtocolHandler handler)
        {
            Db = handler.Db;
            Cache = handler.Cache;
            Rpc = handler.Rpc;
        }

        public virtual Task Run(JsonElement block)
        {
            var ops = block.GetProperty("operations");
            var opsCount = 0;

            if (ops.EnumerateArray().Any())
            {
                opsCount += ops[0].Count() + ops[2].Count();
                foreach (var op in ops[1].EnumerateArray())
                {
                    var content = op.RequiredArray("contents")[0];
                    if (content.RequiredString("kind")[0] == 'p')
                        opsCount += content.RequiredArray("proposals").Count();
                    else
                        opsCount++;
                }
                foreach (var op in ops[3].EnumerateArray())
                {
                    foreach (var content in op.Required("contents").EnumerateArray())
                    {
                        opsCount++;
                        if (content.Required("metadata").TryGetProperty("internal_operation_results", out var internalContents))
                            opsCount += internalContents.Count();
                    }
                }
            }

            return RunDiagnostics(block.Required("header").RequiredInt32("level"), opsCount);
        }

        public virtual Task Run(int level)
        {
            return RunDiagnostics(level);
        }

        protected virtual async Task RunDiagnostics(int level, int ops = -1)
        {
            var entries = Db.ChangeTracker.Entries();

            if (ops != -1 && ops != entries.Count(x => x.Entity is BaseOperation && x.State == EntityState.Added))
                throw new Exception($"Diagnostics failed: wrong operations count");

            var state = Cache.AppState.Get();
            var proto = await Cache.Protocols.GetAsync(state.NextProtocol);

            var accounts = entries.Where(x =>
                x.Entity is Account && (x.State == EntityState.Modified || x.State == EntityState.Added))
                .Select(x => x.Entity as Account);

            await TestGlobalCounter(level, state);

            foreach (var account in accounts)
            {
                if (account is Data.Models.Delegate delegat)
                    await TestDelegate(level, delegat, proto);

                if (account.Type <= AccountType.Contract)
                    await TestAccount(level, account);
            }
            
            if (Cache.Blocks.Current().Events.HasFlag(BlockEvents.CycleBegin))
            {
                foreach (var cycle in entries.Where(x => x.Entity is Cycle).Select(x => x.Entity as Cycle))
                    await TestCycle(state, cycle);
                
                await TestParticipation(state);
                await TestBakersList(state);
                await TestActiveBakersList(state);
            }
        }

        protected virtual Task TestParticipation(AppState state) => Task.CompletedTask;
        
        protected virtual Task TestCycle(AppState state, Cycle cycle) => Task.CompletedTask;

        protected virtual async Task TestBakersList(AppState state)
        {
            var local = Cache.Accounts.GetDelegates().ToList();
            var remote = (await Rpc.GetDelegatesAsync(state.Level)).EnumerateArray()
                .Select(x => x.GetString())
                .ToHashSet();

            if (local.Count != remote.Count)
                throw new Exception("Invalid bakers count");

            foreach (var baker in local)
                if (!remote.Contains(baker.Address))
                    throw new Exception($"Invalid baker {baker.Address}");
        }
        
        protected virtual async Task TestActiveBakersList(AppState state)
        {
            var local = Cache.Accounts.GetDelegates().Where(x => x.Staked).ToList();
            var remote = (await Rpc.GetActiveDelegatesAsync(state.Level)).EnumerateArray()
                .Select(x => x.GetString())
                .ToHashSet();

            if (local.Count != remote.Count)
                throw new Exception("Invalid active bakers count");

            foreach (var baker in local)
                if (!remote.Contains(baker.Address))
                    throw new Exception($"Invalid active baker {baker.Address}");
        }

        protected virtual async Task TestGlobalCounter(int level, AppState state)
        {
            if ((await Rpc.GetGlobalCounterAsync(level)).RequiredInt32() != state.ManagerCounter)
                throw new Exception("Diagnostics failed: wrong global counter");
        }

        protected virtual async Task TestDelegate(int level, Data.Models.Delegate delegat, Protocol proto)
        {
            var remote = await Rpc.GetDelegateAsync(level, delegat.Address);

            if (remote.RequiredInt64("balance") != delegat.Balance)
                throw new Exception($"Diagnostics failed: wrong balance {delegat.Address}");

            if (remote.RequiredBool("deactivated") != !delegat.Staked)
                throw new Exception($"Diagnostics failed: wrong delegate state {delegat.Address}");

            var deactivationCycle = (delegat.DeactivationLevel - 1) >= proto.FirstLevel
                ? proto.GetCycle(delegat.DeactivationLevel - 1)
                : (await Cache.Blocks.GetAsync(delegat.DeactivationLevel - 1)).Cycle;
            if (remote.RequiredInt32("grace_period") != deactivationCycle)
                throw new Exception($"Diagnostics failed: wrong delegate grace period {delegat.Address}");

            if (remote.RequiredInt64("staking_balance") != delegat.StakingBalance)
                throw new Exception($"Diagnostics failed: wrong staking balance {delegat.Address}");

            TestDelegatorsCount(remote, delegat);
        }

        protected virtual void TestDelegatorsCount(JsonElement remote, Data.Models.Delegate local)
        {
            if (remote.RequiredArray("delegated_contracts").Count() != local.DelegatorsCount)
                throw new Exception($"Diagnostics failed: wrong delegators count {local.Address}");
        }

        protected virtual async Task TestAccount(int level, Account account)
        {
            var remote = await Rpc.GetContractAsync(level, account.Address);

            if (account is not Data.Models.Delegate && remote.RequiredInt64("balance") != account.Balance - account.RollupBonds)
                throw new Exception($"Diagnostics failed: wrong balance {account.Address}");

            TestAccountDelegate(remote, account);
            TestAccountCounter(remote, account);
        }

        protected virtual void TestAccountDelegate(JsonElement remote, Account local)
        {
            var remoteDelegate = remote.Required("delegate").OptionalString("value");

            if (local is not Data.Models.Delegate && remoteDelegate != local.Delegate?.Address &&
                !(local is Contract c && (c.Manager == null || c.Manager.Address == remoteDelegate)))
                throw new Exception($"Diagnostics failed: wrong delegate {local.Address}");
        }

        protected virtual void TestAccountCounter(JsonElement remote, Account local)
        {
            if (remote.RequiredInt64("balance") > 0 && remote.RequiredInt32("counter") != local.Counter)
                throw new Exception($"Diagnostics failed: wrong counter {local.Address}");
        }
    }
}
