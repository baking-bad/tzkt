using System.Text.Json;
using Tzkt.Sync.Services;

namespace Tzkt.Sync.Protocols.Initiator
{
    sealed class Rpc(TezosNode node) : Proto1.Rpc(node)
    {
        public override Task<JsonElement> GetBlockAsync(int level)
            => Node.GetAsync($"chains/main/blocks/{level}");
    }
}
