using System.Text.Json;
using Tzkt.Sync.Services;

namespace Tzkt.Sync.Protocols.Proto17
{
    class Rpc : Proto12.Rpc
    {
        public Rpc(TezosNode node) : base(node) { }
        
        public override Task<string> GetTicketBalance(int level, string address, string ticket)
            => Node.PostAsync<string>($"chains/main/blocks/{level}/context/contracts/{address}/ticket_balance", ticket);
    }
}
