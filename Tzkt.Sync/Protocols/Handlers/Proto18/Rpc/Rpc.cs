using System.Text.Json;
using Tzkt.Sync.Services;

namespace Tzkt.Sync.Protocols.Proto18
{
    class Rpc : Proto16.Rpc
    {
        public Rpc(MavrykNode node) : base(node) { }

        public override Task<JsonElement> GetExpectedIssuance(int level)
            => Node.GetAsync($"chains/main/blocks/{level}/context/issuance/expected_issuance");

        public override Task<JsonElement> GetCurrentStakingBalance(int level, string address)
            => Node.GetAsync($"chains/main/blocks/{level}/context/raw/json/staking_balance/current/{address}");

        public override Task<JsonElement> GetStakingParameters(int level, string address)
            => Node.GetAsync($"chains/main/blocks/{level}/context/raw/json/contracts/index/{address}/staking_parameters");
    }
}
