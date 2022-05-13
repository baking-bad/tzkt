using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Netezos.Encoding;
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

            var totalStake = delegates.Sum(x => x.StakingBalance);
            var totalDelegated = delegates.Sum(x => x.DelegatedBalance);
            var totalDelegators = delegates.Sum(x => x.DelegatorsCount);
            var totalBakers = delegates.Count();
            var selectedStake = delegates.Sum(x => x.StakingBalance - x.StakingBalance % protocol.TokensPerRoll);
            var selectedBakers = delegates.Count(x => x.StakingBalance >= protocol.TokensPerRoll);

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
                    TotalStaking = totalStake,
                    TotalDelegated = totalDelegated,
                    TotalDelegators = totalDelegators,
                    TotalBakers = totalBakers,
                    SelectedStake = selectedStake,
                    SelectedBakers = selectedBakers,
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
