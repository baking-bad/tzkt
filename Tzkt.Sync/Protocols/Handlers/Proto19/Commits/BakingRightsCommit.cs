using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto19
{
    class BakingRightsCommit : Proto16.BakingRightsCommit
    {
        public BakingRightsCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override Task ApplyNewCycle(Block block, Cycle futureCycle, Dictionary<int, long> selectedStakes)
        {
            if (block.Cycle == block.Protocol.FirstCycle)
                return Task.CompletedTask;

            return base.ApplyNewCycle(block, futureCycle, selectedStakes);
        }

        public override async Task RevertNewCycle(Block block)
        {
            block.Protocol ??= await Cache.Protocols.GetAsync(block.ProtoCode);

            if (block.Cycle == block.Protocol.FirstCycle)
                return;

            await base.RevertNewCycle(block);
        }
    }
}
