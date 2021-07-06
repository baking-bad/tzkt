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
                        if (content.RequiredString("kind")[0] == 't' &&
                            content.Required("metadata").TryGetProperty("internal_operation_results", out var internalContents))
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
                x.Entity is Account &&
                (x.State == EntityState.Modified ||
                x.State == EntityState.Added))
                .Select(x => x.Entity as Account);

            await TestState(level, state);

            foreach (var account in accounts)
            {
                if (account is Data.Models.Delegate delegat)
                    await TestDelegate(level, delegat, proto);

                await TestAccount(level, account);
            }
        }

        protected virtual async Task TestState(int level, AppState state)
        {
            if ((await Rpc.GetGlobalCounterAsync(level)).RequiredInt32() != state.ManagerCounter)
                throw new Exception($"Diagnostics failed: wrong global counter");
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

            var frozenBalances = remote.RequiredArray("frozen_balance_by_cycle").EnumerateArray();
            
            if ((frozenBalances.Any() ? frozenBalances.Sum(x => x.RequiredInt64("deposit")) : 0) != delegat.FrozenDeposits)
                throw new Exception($"Diagnostics failed: wrong frozen deposits {delegat.Address}");

            if ((frozenBalances.Any() ? frozenBalances.Sum(x => x.RequiredInt64("rewards")) : 0) != delegat.FrozenRewards)
                throw new Exception($"Diagnostics failed: wrong frozen rewards {delegat.Address}");

            if ((frozenBalances.Any() ? frozenBalances.Sum(x => x.RequiredInt64("fees")) : 0) != delegat.FrozenFees)
                throw new Exception($"Diagnostics failed: wrong frozen fees {delegat.Address}");

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

            if (!(account is Data.Models.Delegate) && remote.RequiredInt64("balance") != account.Balance)
                throw new Exception($"Diagnostics failed: wrong balance {account.Address}");

            TestAccountDelegate(remote, account);
            TestAccountCounter(remote, account);
        }

        protected virtual void TestAccountDelegate(JsonElement remote, Account local)
        {
            var remoteDelegate = remote.Required("delegate").OptionalString("value");

            if (!(local is Data.Models.Delegate) && remoteDelegate != local.Delegate?.Address &&
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
