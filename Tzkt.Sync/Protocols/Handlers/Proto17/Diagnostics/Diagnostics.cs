using Netezos.Encoding;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto17
{
    class Diagnostics : Proto14.Diagnostics
    {
        public Diagnostics(ProtocolHandler handler) : base(handler) { }

        protected override async Task TestTicketBalance(int level, TicketBalance balance)
        {
            var a = balance.
            var a = await Rpc.GetTicketBalance(level, balance., ticket);
        }
    }
}
