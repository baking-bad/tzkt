using System.Numerics;
using System.Text.Json;
using Netezos.Encoding;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto16
{
    class Diagnostics : Proto14.Diagnostics
    {
        public Diagnostics(ProtocolHandler handler) : base(handler) { }
        
        protected override async Task TestTicketBalance(int level, TicketBalance balance)
        {
            var ticketer = await Cache.Accounts.GetAsync(balance.TicketerId);
            var ticket = Cache.Tickets.Get(balance.TicketId);
            var account = await Cache.Accounts.GetAsync(balance.AccountId);
            
            var update = new
            {
                ticketer = ticketer.Address,
                content_type = Micheline.FromBytes(ticket.RawType),
                content = Micheline.FromBytes(ticket.RawContent)
            };

            if (BigInteger.TryParse(await Rpc.GetTicketBalance(level, account.Address, JsonSerializer.Serialize(update)), out var remoteBalance))
            {
                if (remoteBalance != balance.Balance)
                    throw new Exception($"Diagnostics failed: wrong ticket balance for {account.Address}");
            }
            else
            {
                throw new Exception("Failed to get ticket balance");
            }
        }
    }
}
