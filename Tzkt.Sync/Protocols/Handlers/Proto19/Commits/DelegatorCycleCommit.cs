using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto19
{
    class DelegatorCycleCommit : Proto18.DelegatorCycleCommit
    {
        public DelegatorCycleCommit(ProtocolHandler protocol) : base(protocol) { }

        public override Task Apply(Block block, Cycle futureCycle)
        {
            if (block.Cycle == block.Protocol.FirstCycle)
                return Task.CompletedTask;

            return base.Apply(block, futureCycle);
        }

        public override async Task Revert(Block block)
        {
            if (!block.Events.HasFlag(BlockEvents.CycleBegin))
                return;

            block.Protocol ??= await Cache.Protocols.GetAsync(block.ProtoCode);

            if (block.Cycle == block.Protocol.FirstCycle)
                return;

            await base.Revert(block);
        }
    }
}
