using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto19
{
    class BakerCycleCommit : Proto18.BakerCycleCommit
    {
        public BakerCycleCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override async Task ApplyNewCycle(
            Block block,
            Cycle futureCycle,
            IEnumerable<RightsGenerator.BR> futureBakingRights,
            IEnumerable<RightsGenerator.AR> futureAttestationRights,
            List<SnapshotBalance> snapshots,
            Dictionary<int, long> selectedStakes)
        {
            if (block.Cycle == Context.Protocol.FirstCycle)
            {
                var prevProto = await Cache.Protocols.GetAsync(Context.Protocol.Code - 1);
                if (prevProto.ConsensusRightsDelay != Context.Protocol.ConsensusRightsDelay)
                    return;
            }

            await base.ApplyNewCycle(block, futureCycle, futureBakingRights, futureAttestationRights, snapshots, selectedStakes);
        }

        protected override async Task RevertNewCycle(Block block)
        {
            if (block.Cycle == Context.Protocol.FirstCycle)
            {
                var prevProto = await Cache.Protocols.GetAsync(Context.Protocol.Code - 1);
                if (prevProto.ConsensusRightsDelay != Context.Protocol.ConsensusRightsDelay)
                    return;
            }

            await base.RevertNewCycle(block);
        }
    }
}
