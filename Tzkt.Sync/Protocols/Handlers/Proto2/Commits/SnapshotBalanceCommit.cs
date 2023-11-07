using System.Text.Json;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto2
{
    class SnapshotBalanceCommit : Proto1.SnapshotBalanceCommit
    {
        public SnapshotBalanceCommit(ProtocolHandler protocol) : base(protocol) { }

        public override async Task Apply(JsonElement rawBlock, Block block)
        {
            if (!block.Events.HasFlag(BlockEvents.BalanceSnapshot))
                return;

            await RemoveOutdated(block, block.Protocol);
            await TakeSnapshot(block);
            await TakeDeactivatedSnapshot(block);
            await SubtractCycleRewards(rawBlock, block);
        }
    }
}
