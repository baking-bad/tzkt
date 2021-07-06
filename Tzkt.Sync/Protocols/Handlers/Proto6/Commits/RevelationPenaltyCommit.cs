using System.Linq;
using System.Text.Json;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto6
{
    class RevelationPenaltyCommit : Proto4.RevelationPenaltyCommit
    {
        public RevelationPenaltyCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override bool HasPanltiesUpdates(Block block, JsonElement rawBlock)
        {
            return rawBlock
                .Required("metadata")
                .RequiredArray("balance_updates")
                .EnumerateArray()
                .Skip(block.Cycle < block.Protocol.NoRewardCycles || rawBlock.Required("operations")[0].Count() == 0 ? 2 : 3)
                .Any(x => x.RequiredString("kind")[0] == 'f' && GetFreezerCycle(x) != block.Cycle - block.Protocol.PreservedCycles);
        }
    }
}
