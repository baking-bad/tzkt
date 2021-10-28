using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto9
{
    class FreezerCommit : Proto6.FreezerCommit
    {
        public FreezerCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override IEnumerable<JsonElement> GetFreezerUpdates(Block block, JsonElement rawBlock)
        {
            return rawBlock
                .Required("metadata")
                .Required("balance_updates")
                .EnumerateArray()
                .Where(x => x.RequiredString("origin")[0] == 'b' &&
                            x.RequiredString("kind")[0] == 'f' &&
                            x.RequiredInt64("change") < 0 &&
                            GetFreezerCycle(x) == block.Cycle - block.Protocol.PreservedCycles);
        }
    }
}
