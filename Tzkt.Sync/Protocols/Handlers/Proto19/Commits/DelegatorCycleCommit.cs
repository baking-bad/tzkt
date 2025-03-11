using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto19
{
    class DelegatorCycleCommit(ProtocolHandler protocol) : Proto18.DelegatorCycleCommit(protocol)
    {
        public override async Task Apply(Block block, Cycle? futureCycle)
        {
            if (block.Cycle == Context.Protocol.FirstCycle)
            {
                var prevProto = await Cache.Protocols.GetAsync(Context.Protocol.Code - 1);
                if (prevProto.ConsensusRightsDelay != Context.Protocol.ConsensusRightsDelay)
                    return;
            }

            await base.Apply(block, futureCycle);
        }

        public override async Task Revert(Block block)
        {
            if (!block.Events.HasFlag(BlockEvents.CycleBegin))
                return;

            if (block.Cycle == Context.Protocol.FirstCycle)
            {
                var prevProto = await Cache.Protocols.GetAsync(Context.Protocol.Code - 1);
                if (prevProto.ConsensusRightsDelay != Context.Protocol.ConsensusRightsDelay)
                    return;
            }

            await base.Revert(block);
        }
    }
}
