using System.Text.Json;
using Netezos.Contracts;
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

            var res = new List<TicketUpdate>();
            foreach (var update in  ticketUpdates.RequiredArray().EnumerateArray())
            {
                try
                {
                    var ticketToken = update.Required("ticket_token");
                    var micheType = Schema.Create(Micheline.FromJson(ticketToken.Required("content_type")) as MichelinePrim);
                    var value = Micheline.FromJson(ticketToken.Required("content"));
                    var rawContent = micheType.Optimize(value).ToBytes();
                    var rawType = micheType.ToMicheline().ToBytes();
                    res.Add(new TicketUpdate
                    {
                        TicketToken = new TicketToken
                        {
                            Ticketer = ticketToken.RequiredString("ticketer"),
                            RawType = rawType,
                            RawContent = rawContent,
                            JsonContent = micheType.Humanize(value),
                            ContentTypeHash = Script.GetHash(rawType),
                            ContentHash = Script.GetHash(rawContent)
                        },
                        Updates = update.Required("updates").RequiredArray().EnumerateArray().Select(y => new Update
                        {
                            Account = y.RequiredString("account"),
                            Amount = y.RequiredString("amount")
                        })
                    });
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "failed to process 'transfer_ticket' parameters");
                }
            }

            return res;
        }
    }
}
