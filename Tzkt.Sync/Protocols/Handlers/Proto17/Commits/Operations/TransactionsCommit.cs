using System.Text.Json;
using Netezos.Encoding;

namespace Tzkt.Sync.Protocols.Proto17
{
    class TransactionsCommit : Proto14.TransactionsCommit
    {
        public TransactionsCommit(ProtocolHandler protocol) : base(protocol) { }
        
        protected override IEnumerable<TicketUpdate> ParseTicketUpdates(string property, JsonElement result)
        {
            if (!result.TryGetProperty("ticket_receipt", out var ticketUpdates))
                return null;

            return ticketUpdates.RequiredArray().EnumerateArray().Select(x => new TicketUpdate
            {
                TicketToken = x.TryGetProperty("ticket_token", out var ticketToken)
                    ? new TicketToken
                    {
                        Ticketer = ticketToken.RequiredString("ticketer"),
                        ContentType = ticketToken.TryGetProperty("content_type", out var contentType)
                            ? Micheline.FromJson(contentType)
                            : null,
                        Content = ticketToken.TryGetProperty("content", out var content)
                            ? Micheline.FromJson(content)
                            : null,
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
