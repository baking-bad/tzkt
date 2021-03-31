using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto1
{
    partial class ProtoActivator : ProtocolCommit
    {
        public async Task BootstrapCycles(Protocol protocol, List<Account> accounts)
        {
            var delegates = accounts
                .Where(x => x.Type == AccountType.Delegate)
                .Select(x => x as Delegate);

            var totalRolls = delegates.Sum(x => (int)(x.StakingBalance / protocol.TokensPerRoll));
            var totalStake = delegates.Sum(x => x.StakingBalance);
            var totalDelegated = delegates.Sum(x => x.StakingBalance - x.Balance);
            var totalDelegators = delegates.Sum(x => x.DelegatorsCount);
            var totalBakers = delegates.Count();

            for (int cycle = 0; cycle <= protocol.PreservedCycles; cycle++)
            {
                var rawCycle = await Proto.Rpc.GetCycleAsync(1, cycle);
                Db.Cycles.Add(new Cycle
                {
                    Index = cycle,
                    FirstLevel = cycle * protocol.BlocksPerCycle + 1,
                    LastLevel = (cycle + 1) * protocol.BlocksPerCycle,
                    SnapshotIndex = 0,
                    SnapshotLevel = 1,
                    TotalRolls = totalRolls,
                    TotalStaking = totalStake,
                    TotalDelegated = totalDelegated,
                    TotalDelegators = totalDelegators,
                    TotalBakers = totalBakers,
                    Seed = rawCycle.RequiredString("random_seed")
                });
            }

            var state = Cache.AppState.Get();
            state.CyclesCount += protocol.PreservedCycles + 1;
        }

        public async Task ClearCycles()
        {
            await Db.Database.ExecuteSqlRawAsync(@"DELETE FROM ""Cycles""");
            var state = Cache.AppState.Get();
            state.CyclesCount = 0;
        }
    }
}
