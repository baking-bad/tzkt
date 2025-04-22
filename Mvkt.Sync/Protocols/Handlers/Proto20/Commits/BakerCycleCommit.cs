using Mvkt.Data.Models;

namespace Mvkt.Sync.Protocols.Proto20
{
    class BakerCycleCommit : Proto18.BakerCycleCommit
    {
        public BakerCycleCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override async Task ApplyNewCycle(
            Block block,
            Cycle futureCycle,
            IEnumerable<RightsGenerator.BR> futureBakingRights,
            IEnumerable<RightsGenerator.ER> futureEndorsingRights,
            List<SnapshotBalance> snapshots,
            Dictionary<int, long> selectedStakes)
        {
            if (block.Cycle == block.Protocol.FirstCycle)
            {
                var prevProto = await Cache.Protocols.GetAsync(block.Protocol.Code - 1);
                if (prevProto.ConsensusRightsDelay != block.Protocol.ConsensusRightsDelay)
                    return;
            }

            await base.ApplyNewCycle(block, futureCycle, futureBakingRights, futureEndorsingRights, snapshots, selectedStakes);
        }

        protected override async Task RevertNewCycle(Block block)
        {
            block.Protocol ??= await Cache.Protocols.GetAsync(block.ProtoCode);

            if (block.Cycle == block.Protocol.FirstCycle)
            {
                var prevProto = await Cache.Protocols.GetAsync(block.Protocol.Code - 1);
                if (prevProto.ConsensusRightsDelay != block.Protocol.ConsensusRightsDelay)
                    return;
            }

            await base.RevertNewCycle(block);
        }
    }
}
