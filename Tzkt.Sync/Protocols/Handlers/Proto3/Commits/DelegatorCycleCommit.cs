using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto3
{
    class DelegatorCycleCommit(ProtocolHandler protocol) : Proto1.DelegatorCycleCommit(protocol)
    {
        public override async Task Apply(Block block, Cycle? futureCycle)
        {
            if (!block.Events.HasFlag(BlockEvents.CycleBegin))
                return;

            await CreateFromSnapshots(futureCycle!);
        }
    }
}
