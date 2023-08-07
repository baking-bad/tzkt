using System.Text.Json;
using Netezos.Encoding;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto17
{
    class TransactionsCommit : Proto14.TransactionsCommit
    {
        public TransactionsCommit(ProtocolHandler protocol) : base(protocol) { }
        
        protected override IEnumerable<TicketUpdate> ParseTicketUpdates(string property, JsonElement result)
        {
            if (!result.TryGetProperty(property, out var ticketUpdates))
                return null;

            return ticketUpdates.RequiredArray().EnumerateArray().Select(x => new TicketUpdate
            {
                TicketToken = x.TryGetProperty("ticket_token", out var ticketToken)
                    ? new TicketToken
                    {
                        Ticketer = ticketToken.RequiredString("ticketer"),
                        ContentType = Micheline.FromJson(ticketToken.Required("content_type")),
                        Content = Micheline.FromJson(ticketToken.Required("content")),
                        ContentTypeHash = Script.GetHash(Micheline.FromJson(ticketToken.Required("content_type")).ToBytes()),
                        ContentHash = Script.GetHash(Micheline.FromJson(ticketToken.Required("content")).ToBytes())
                    }
                    : null,
                Updates = x.TryGetProperty("updates", out var updates)
                ? updates.RequiredArray().EnumerateArray().Select(y => new Update
                {
                    Account = y.RequiredString("account"),
                    Amount = y.RequiredString("amount")
                })
                : null
            });
        }
    }
}
