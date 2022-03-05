using System.Text.Json;
using System.Threading.Tasks;
using Tzkt.Sync.Services;

namespace Tzkt.Sync.Protocols.Proto12
{
    class Rpc : Proto6.Rpc
    {
        public Rpc(TezosNode node) : base(node) { }

        public override Task<JsonElement> GetStakeDistribution(int block, int cycle)
            => Node.GetAsync($"chains/main/blocks/{block}/context/raw/json/cycle/{cycle}/selected_stake_distribution");
    }
}
