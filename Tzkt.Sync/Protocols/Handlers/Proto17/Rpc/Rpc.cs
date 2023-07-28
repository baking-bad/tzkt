using System.Text.Json;
using Tzkt.Sync.Services;

namespace Tzkt.Sync.Protocols.Proto17
{
    class Rpc : Proto12.Rpc
    {
        public Rpc(TezosNode node) : base(node) { }
        
        public override Task<JsonElement> GetTicketBalance(int level, string address, TicketToken ticket)
            => Node.PostAsync<JsonElement>($"chains/main/blocks/{level}/context/contracts/{address}/ticket_balance", ticket);
    }
}
