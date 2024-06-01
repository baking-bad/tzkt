using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto19
{
    class DelegatorCycleCommit : Proto18.DelegatorCycleCommit
    {
        public DelegatorCycleCommit(ProtocolHandler protocol) : base(protocol) { }

        public override async Task Apply(Block block, Cycle futureCycle)
        {
            if (block.Cycle == block.Protocol.FirstCycle)
            {
                var prevProto = await Cache.Protocols.GetAsync(block.Protocol.Code - 1);
                if (prevProto.ConsensusRightsDelay != block.Protocol.ConsensusRightsDelay)
                    return;
            }

            await base.Apply(block, futureCycle);
        }

        public override async Task Revert(Block block)
        {
            if (!block.Events.HasFlag(BlockEvents.CycleBegin))
                return;

            block.Protocol ??= await Cache.Protocols.GetAsync(block.ProtoCode);

            if (block.Cycle == block.Protocol.FirstCycle)
            {
                var prevProto = await Cache.Protocols.GetAsync(block.Protocol.Code - 1);
                if (prevProto.ConsensusRightsDelay != block.Protocol.ConsensusRightsDelay)
                    return;
            }

            await base.Revert(block);
        }
    }
}
