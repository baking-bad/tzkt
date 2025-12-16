using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto24
{
    class BakerCycleCommit(ProtocolHandler protocol) : Proto19.BakerCycleCommit(protocol)
    {
        public override async Task Apply(Block block, Cycle? futureCycle, IEnumerable<RightsGenerator.BR>? futureBakingRights, IEnumerable<RightsGenerator.AR>? futureAttestationRights, List<SnapshotBalance>? snapshots, Dictionary<int, long>? selectedStakes, List<BakingRight> currentRights)
        {
            if (Cache.AppState.Get().AbaActivationLevel == block.Level)
            {
                var state = Cache.AppState.Get();
                var cycles = await Db.Cycles.Where(x => x.Index >= block.Cycle).ToListAsync();
                foreach (var cycle in cycles)
                {
                    var bakerCycles = await Cache.BakerCycles.GetAsync(cycle.Index);
                    foreach (var bakerCycle in bakerCycles.Values)
                    {
                        Db.TryAttach(bakerCycle);
                        bakerCycle.FutureAttestationRewards = GetFutureAttestationRewards(Context.Protocol, cycle, bakerCycle.BakingPower);
                    }
                }
            }

            await base.Apply(block, futureCycle, futureBakingRights, futureAttestationRights, snapshots, selectedStakes, currentRights);
        }

        public override async Task Revert(Block block)
        {
            await base.Revert(block);

            if (Cache.AppState.Get().AbaActivationLevel == block.Level)
            {
                var state = Cache.AppState.Get();
                var cycles = await Db.Cycles.Where(x => x.Index >= block.Cycle).ToListAsync();
                foreach (var cycle in cycles)
                {
                    var bakerCycles = await Cache.BakerCycles.GetAsync(cycle.Index);
                    foreach (var bakerCycle in bakerCycles.Values)
                    {
                        Db.TryAttach(bakerCycle);
                        bakerCycle.FutureAttestationRewards = base.GetFutureAttestationRewards(Context.Protocol, cycle, bakerCycle.BakingPower);
                    }
                }
            }
        }

        protected override long GetFutureAttestationRewards(Protocol protocol, Cycle cycle, long bakingPower)
        {
            if (Cache.AppState.Get().AbaActivationLevel is not null)
                return (protocol.BlocksPerCycle * cycle.AttestationRewardPerBlock).MulRatioUp(bakingPower, cycle.TotalBakingPower);

            return base.GetFutureAttestationRewards(protocol, cycle, bakingPower);
        }
    }
}
