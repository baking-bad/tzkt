using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto19
{
    class BakerCycleCommit : Proto18.BakerCycleCommit
    {
        public BakerCycleCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override Task ApplyNewCycle(
            Block block,
            Cycle futureCycle,
            IEnumerable<RightsGenerator.BR> futureBakingRights,
            IEnumerable<RightsGenerator.ER> futureEndorsingRights,
            List<SnapshotBalance> snapshots,
            Dictionary<int, long> selectedStakes)
        {
            if (block.Cycle == block.Protocol.FirstCycle)
                return Task.CompletedTask;

            return base.ApplyNewCycle(block, futureCycle, futureBakingRights, futureEndorsingRights, snapshots, selectedStakes);
        }

        protected override async Task RevertNewCycle(Block block)
        {
            block.Protocol ??= await Cache.Protocols.GetAsync(block.ProtoCode);
            
            if (block.Cycle == block.Protocol.FirstCycle)
                return;

            await base.RevertNewCycle(block);
        }
    }
}
