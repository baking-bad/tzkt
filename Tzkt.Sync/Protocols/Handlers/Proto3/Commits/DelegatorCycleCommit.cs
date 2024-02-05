using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto3
{
    class DelegatorCycleCommit : Proto1.DelegatorCycleCommit
    {
        public DelegatorCycleCommit(ProtocolHandler protocol) : base(protocol) { }

        public override async Task Apply(Block block, Cycle futureCycle)
        {
            if (!block.Events.HasFlag(BlockEvents.CycleBegin))
                return;

            await CreateFromSnapshots(futureCycle);
        }
    }
}
