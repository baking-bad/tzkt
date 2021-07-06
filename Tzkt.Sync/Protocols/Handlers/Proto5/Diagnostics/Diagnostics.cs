using System;
using System.Text.Json;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto5
{
    class Diagnostics : Proto1.Diagnostics
    {
        public Diagnostics(ProtocolHandler handler) : base(handler) { }

        protected override void TestDelegatorsCount(JsonElement remote, Data.Models.Delegate local)
        {
            var delegators = remote.RequiredArray("delegated_contracts").Count();

            if (delegators != local.DelegatorsCount && delegators != local.DelegatorsCount + 1)
                throw new Exception($"Diagnostics failed: wrong delegators count {local.Address}");
        }

        protected override void TestAccountDelegate(JsonElement remote, Account local)
        {
            var remoteDelegate = remote.OptionalString("delegate");

            if (!(local is Data.Models.Delegate) && remoteDelegate != local.Delegate?.Address &&
                !(local is Contract c && (c.Manager == null || c.Manager.Address == remoteDelegate)))
                throw new Exception($"Diagnostics failed: wrong delegate {local.Address}");
        }

        protected override void TestAccountCounter(JsonElement remote, Account local)
        {
            if (local.Type == AccountType.Contract) return;

            if (remote.RequiredInt64("balance") > 0 && remote.RequiredInt32("counter") != local.Counter)
                throw new Exception($"Diagnostics failed: wrong counter {local.Address}");
        }
    }
}
