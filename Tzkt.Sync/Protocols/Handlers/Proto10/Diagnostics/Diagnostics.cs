using System.Text.Json;

namespace Tzkt.Sync.Protocols.Proto10
{
    class Diagnostics : Proto5.Diagnostics
    {
        public Diagnostics(ProtocolHandler handler) : base(handler) { }

        protected override long GetDeposits(JsonElement json) => json.RequiredInt64("deposits");
    }
}
