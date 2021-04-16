using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Tzkt.Sync.Protocols.Proto9
{
    class SnapshotBalanceCommit : Proto4.SnapshotBalanceCommit
    {
        public SnapshotBalanceCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override IEnumerable<JsonElement> GetBalanceUpdates(JsonElement rawBlock)
        {
            return rawBlock
                .GetProperty("metadata")
                .GetProperty("balance_updates")
                .EnumerateArray()
                .Where(x => x.RequiredString("origin")[0] == 'b');
        }
    }
}
