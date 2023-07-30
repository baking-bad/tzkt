using System.Numerics;
using System.Text.Json;
using Netezos.Encoding;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto17
{
    class Diagnostics : Proto14.Diagnostics
    {
        public Diagnostics(ProtocolHandler handler) : base(handler) { }

        protected override async Task TestTicketBalance(int level, TicketBalance balance)
        {
            var update = new
            {
                ticketer = balance.Ticketer.Address,
                content_type = Micheline.FromBytes(balance.Ticket.ContentType),
                content = Micheline.FromBytes(balance.Ticket.Content)
            };
            var ticket = JsonSerializer.Serialize(update);

            if (BigInteger.TryParse((await Rpc.GetTicketBalance(level, balance.Account.Address, ticket)).ToString(), out var remoteBalance))
            {
                if (remoteBalance != balance.Balance)
                    throw new Exception($"Diagnostics failed: wrong ticket balance for {balance.Account.Address}");
            }
            else
            {
                throw new Exception("Failed to get ticket balance");
            }
        }
    }
}
