using System.Text.Json;
using Mvkt.Data.Models;

namespace Mvkt.Sync.Protocols.Proto20
{
    class SnapshotBalanceCommit : Proto19.SnapshotBalanceCommit
    {
        public SnapshotBalanceCommit(ProtocolHandler protocol) : base(protocol) { }

        public override async Task Apply(JsonElement rawBlock, Block block)
        {
            if (!block.Events.HasFlag(BlockEvents.BalanceSnapshot))
                return;

            await RemoveOutdated(block, block.Protocol);
            await TakeSnapshot(block);
        }
    }
}
