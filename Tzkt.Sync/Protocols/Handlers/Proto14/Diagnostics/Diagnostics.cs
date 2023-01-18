using System.Text.Json;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto14
{
    class Diagnostics : Proto12.Diagnostics
    {
        public Diagnostics(ProtocolHandler handler) : base(handler) { }

        protected override bool CheckDelegatedBalance(JsonElement remote, Data.Models.Delegate delegat) =>
            remote.RequiredInt64("delegated_balance") == delegat.DelegatedBalance;
    }
}
