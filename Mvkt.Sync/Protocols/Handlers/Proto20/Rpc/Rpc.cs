using System.Text.Json;
using Mvkt.Sync.Services;

namespace Mvkt.Sync.Protocols.Proto20
{
    class Rpc : Proto19.Rpc
    {
        public Rpc(MavrykNode node) : base(node) { }

        public override Task<JsonElement> GetBlockAsync(int level)
            => Node.GetAsync($"chains/main/blocks/{level}");

        public override Task<JsonElement> GetCurrentStakingBalance(int level, string address)
            => Node.GetAsync($"chains/main/blocks/{level}/context/raw/json/staking_balance/{address}");
    }
}
