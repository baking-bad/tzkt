using System.Linq;
using System.Text.Json;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto6
{
    class RevelationPenaltyCommit : Proto4.RevelationPenaltyCommit
    {
        public RevelationPenaltyCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override bool HasPenaltiesUpdates(Block block, JsonElement rawBlock)
        {
            return rawBlock
                .Required("metadata")
                .RequiredArray("balance_updates")
                .EnumerateArray()
                .Any(x => x.RequiredString("kind")[0] == 'f' &&
                          x.RequiredInt64("change") < 0 &&
                          GetFreezerCycle(x) != block.Cycle - block.Protocol.PreservedCycles);
        }
    }
}
