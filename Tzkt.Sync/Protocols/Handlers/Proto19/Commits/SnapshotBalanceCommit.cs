using System.Text.Json;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto19
{
    class SnapshotBalanceCommit : Proto18.SnapshotBalanceCommit
    {
        public SnapshotBalanceCommit(ProtocolHandler protocol) : base(protocol) { }

        public override async Task Apply(JsonElement rawBlock, Block block)
        {
            if (!block.Events.HasFlag(BlockEvents.BalanceSnapshot))
                return;

            await RemoveOutdated(block, Context.Protocol);
            await TakeSnapshot(block);
        }
    }
}
