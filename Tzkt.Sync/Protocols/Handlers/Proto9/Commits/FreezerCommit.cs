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
            var cycle = (block.Level - 1) / block.Protocol.BlocksPerCycle;
            return rawBlock
                .Required("metadata")
                .Required("balance_updates")
                .EnumerateArray()
                .Where(x => x.RequiredString("origin")[0] == 'b')
                .Skip(cycle < block.Protocol.NoRewardCycles || rawBlock.Required("operations")[0].Count() == 0 ? 2 : 3)
                .Where(x => x.RequiredString("kind")[0] == 'f' && GetFreezerCycle(x) == cycle - block.Protocol.PreservedCycles);
        }
    }
}
