using System.Linq;
using System.Text.Json;

namespace Tzkt.Sync.Protocols.Proto9
{
    class BlockCommit : Proto1.BlockCommit
    {
        public BlockCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override JsonElement GetBlockReward(JsonElement metadata)
        {
            return metadata
                .RequiredArray("balance_updates")
                .EnumerateArray()
                .Where(x => x.RequiredString("origin")[0] == 'b')
                .Take(3)
                .FirstOrDefault(x => x.RequiredString("kind")[0] == 'f' && x.RequiredString("category")[0] == 'r');
        }

        protected override JsonElement GetBlockDeposit(JsonElement metadata)
        {
            return metadata
                .RequiredArray("balance_updates")
                .EnumerateArray()
                .Where(x => x.RequiredString("origin")[0] == 'b')
                .Take(3)
                .FirstOrDefault(x => x.RequiredString("kind")[0] == 'f' && x.RequiredString("category")[0] == 'd');
        }
    }
}
