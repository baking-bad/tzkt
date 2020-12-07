using System.Text.Json;

namespace Tzkt.Sync.Protocols.Proto3
{
    class TransactionsCommit : Proto1.TransactionsCommit
    {
        public TransactionsCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override bool HasAllocated(JsonElement result) => result.OptionalBool("allocated_destination_contract") ?? false;
    }
}
