using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto10
{
    class BakingRightsCommit : Proto3.BakingRightsCommit
    {
        public BakingRightsCommit(ProtocolHandler protocol) : base(protocol) { }

        // Tezos node is no longer able to return endorsing rights for the whole cycle, so we have to find another way...
        protected override async Task<IEnumerable<JsonElement>> GetEndorsingRights(Block block, int cycle)
        {
            var rights = new List<JsonElement>(block.Protocol.BlocksPerCycle * block.Protocol.EndorsersPerBlock / 2);
            var firstLevel = block.Protocol.GetCycleStart(cycle);
            var lastLevel = block.Protocol.GetCycleEnd(cycle);

            for (int level = firstLevel; level <= lastLevel; level++)
                rights.AddRange((await Proto.Rpc.GetLevelEndorsingRightsAsync(block.Level, level)).RequiredArray().EnumerateArray());

            if (!rights.Any() || rights.Sum(x => x.RequiredArray("slots").Count()) != block.Protocol.BlocksPerCycle * block.Protocol.EndorsersPerBlock)
                throw new ValidationException("Rpc returned less endorsing rights (slots) than it should be");

            return rights;
        }
    }
}
