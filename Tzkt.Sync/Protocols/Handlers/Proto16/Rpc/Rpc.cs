using System.Text.Json;
using Tzkt.Sync.Services;

namespace Tzkt.Sync.Protocols.Proto16
{
    class Rpc : Proto12.Rpc
    {
        public Rpc(MavrykNode node) : base(node) { }

        public override Task<JsonElement> GetTicketBalance(int level, string address, string ticket)
        {
            return address.StartsWith("sr1")
                ? Node.PostAsync<JsonElement>($"chains/main/blocks/{level}/context/smart_rollups/smart_rollup/{address}/ticket_balance", ticket)
                : Node.PostAsync<JsonElement>($"chains/main/blocks/{level}/context/contracts/{address}/ticket_balance", ticket);
        }
    }
}
