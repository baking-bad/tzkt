using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto21
{
    class CycleCommit : Proto20.CycleCommit
    {
        public CycleCommit(ProtocolHandler protocol) : base(protocol) { }

        public override async Task Apply(Block block)
        {
            if (block.Cycle == block.Protocol.FirstCycle)
            {
                var prevProto = await Cache.Protocols.GetAsync(block.Protocol.Code - 1);
                if (prevProto.ConsensusRightsDelay != block.Protocol.ConsensusRightsDelay)
                {
                    Cache.AppState.Get().CyclesCount--;
                    return;
                }
            }

            await base.Apply(block);
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
                {
                    Cache.AppState.Get().CyclesCount++;
                    return;
                }
            }

            await base.Revert(block);
        }
    }
}
