using System.Text.Json;

namespace Tzkt.Sync.Protocols.Proto3
{
    class TransactionsCommit : Proto2.TransactionsCommit
    {
        public TransactionsCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override bool HasAllocated(JsonElement result) => result.OptionalBool("allocated_destination_contract") ?? false;
    }
}
