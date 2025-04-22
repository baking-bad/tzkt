using Mvkt.Data.Models;

namespace Mvkt.Sync.Protocols.Proto20
{
    class BakingRightsCommit : Proto18.BakingRightsCommit
    {
        public BakingRightsCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override async Task ApplyNewCycle(Block block, Cycle futureCycle, Dictionary<int, long> selectedStakes)
        {
            if (block.Cycle == block.Protocol.FirstCycle)
            {
                var prevProto = await Cache.Protocols.GetAsync(block.Protocol.Code - 1);
                if (prevProto.ConsensusRightsDelay != block.Protocol.ConsensusRightsDelay)
                    return;
            }

            await base.ApplyNewCycle(block, futureCycle, selectedStakes);
        }

        public override async Task RevertNewCycle(Block block)
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
