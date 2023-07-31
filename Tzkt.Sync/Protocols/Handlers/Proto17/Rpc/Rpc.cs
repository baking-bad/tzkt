using System.Text.Json;
using System.Text.RegularExpressions;
using Tzkt.Sync.Services;

namespace Tzkt.Sync.Protocols.Proto17
{
    class Rpc : Proto12.Rpc
    {
        public Rpc(TezosNode node) : base(node) { }

        public override Task<string> GetTicketBalance(int level, string address, string ticket)
        {
            return Regex.IsMatch(address, "^sr1[0-9A-Za-z]{33}$") 
                ? Node.PostAsync<string>($"chains/main/blocks/{level}/context/smart_rollups/smart_rollup/{address}/ticket_balance", ticket)
                : Node.PostAsync<string>($"chains/main/blocks/{level}/context/contracts/{address}/ticket_balance", ticket);
        }
    }
}
