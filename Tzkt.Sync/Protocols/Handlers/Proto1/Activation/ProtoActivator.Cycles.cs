using Microsoft.EntityFrameworkCore;
using Netmavryk.Encoding;
using Newtonsoft.Json.Linq;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto1
{
    partial class ProtoActivator : ProtocolCommit
    {
        public virtual List<Cycle> BootstrapCycles(Protocol protocol, List<Account> accounts, JToken parameters)
        {
            var cycles = new List<Cycle>(protocol.PreservedCycles + 1);
            var delegates = accounts
                .Where(x => x.Type == AccountType.Delegate)
                .Select(x => x as Data.Models.Delegate);
            var selected = delegates.Where(x => x.StakingBalance >= protocol.MinimalStake);
            var selectedBakers = selected.Count();
            var selectedStaking = selected.Sum(x => x.StakingBalance - x.StakingBalance % protocol.MinimalStake);

            var base58Seed = parameters["initial_seed"]?.Value<string>();
            if (!Base58.TryParse(base58Seed, new byte[3], out var initialSeed) || initialSeed.Length != 32)
                initialSeed = Array.Empty<byte>();

            var seeds = Seed.GetInitialSeeds(protocol.PreservedCycles + 1, initialSeed);
            for (int index = 0; index <= protocol.PreservedCycles; index++)
            {
                var cycle = new Cycle
                {
                    Index = index,
                    FirstLevel = protocol.GetCycleStart(index),
                    LastLevel = protocol.GetCycleEnd(index),
                    SnapshotIndex = 0,
                    SnapshotLevel = 1,
                    TotalBakers = selectedBakers,
                    TotalBakingPower = selectedStaking,
                    Seed = seeds[index]
                };
                Db.Cycles.Add(cycle);
                cycles.Add(cycle);
            }

            var state = Cache.AppState.Get();
            state.CyclesCount += protocol.PreservedCycles + 1;

            return cycles;
        }

        public async Task ClearCycles()
        {
            await Db.Database.ExecuteSqlRawAsync(@"DELETE FROM ""Cycles""");
            var state = Cache.AppState.Get();
            state.CyclesCount = 0;
        }
    }
}
