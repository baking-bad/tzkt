using System.Text.Json;

namespace Tzkt.Sync.Protocols.Proto4
{
    class SnapshotBalanceCommit : Proto2.SnapshotBalanceCommit
    {
        public SnapshotBalanceCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override int GetFreezerCycle(JsonElement el) => el.RequiredInt32("cycle");
    }
}
