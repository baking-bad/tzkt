using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto1
{
    partial class ProtoActivator : ProtocolCommit
    {
        public void BootstrapCycles(Protocol protocol, List<Account> accounts)
        {
            var delegates = accounts
                .Where(x => x.Type == AccountType.Delegate)
                .Select(x => x as Delegate);

            var totalStake = delegates.Sum(x => x.StakingBalance);
            var totalDelegated = delegates.Sum(x => x.DelegatedBalance);
            var totalDelegators = delegates.Sum(x => x.DelegatorsCount);
            var totalBakers = delegates.Count();
            var selectedStake = delegates.Sum(x => x.StakingBalance - x.StakingBalance % protocol.TokensPerRoll);
            var selectedBakers = delegates.Count(x => x.StakingBalance >= protocol.TokensPerRoll);

            var seeds = Seed.GetInitialSeeds(protocol.PreservedCycles + 1);
            for (int index = 0; index <= protocol.PreservedCycles; index++)
            {
                Db.Cycles.Add(new Cycle
                {
                    Index = index,
                    FirstLevel = protocol.GetCycleStart(index),
                    LastLevel = protocol.GetCycleEnd(index),
                    SnapshotIndex = 0,
                    SnapshotLevel = 1,
                    TotalStaking = totalStake,
                    TotalDelegated = totalDelegated,
                    TotalDelegators = totalDelegators,
                    TotalBakers = totalBakers,
                    SelectedStake = selectedStake,
                    SelectedBakers = selectedBakers,
                    Seed = seeds[index]
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
