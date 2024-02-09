using System.Text.Json;
using Mvkt.Sync.Services;

namespace Mvkt.Sync.Protocols.Proto19
{
    class Rpc : Proto18.Rpc
    {
        public Rpc(TezosNode node) : base(node) { }

        public override Task<JsonElement> GetBlockAsync(int level)
            => Node.GetAsync($"chains/main/blocks/{level}");

        public override Task<JsonElement> GetCurrentStakingBalance(int level, string address)
            => Node.GetAsync($"chains/main/blocks/{level}/context/raw/json/staking_balance/{address}");
    }
}
