using System.Text.Json;
using Tzkt.Sync.Services;

namespace Tzkt.Sync.Protocols.Initiator
{
    sealed class Rpc : Proto1.Rpc
    {
        public Rpc(TezosNode node) : base(node) { }

        public override Task<JsonElement> GetBlockAsync(int level)
            => Node.GetAsync($"chains/main/blocks/{level}");
    }
}
