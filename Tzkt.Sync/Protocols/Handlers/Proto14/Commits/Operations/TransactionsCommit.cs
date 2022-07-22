using System.Text.Json;

namespace Tzkt.Sync.Protocols.Proto14
{
    class TransactionsCommit : Proto13.TransactionsCommit
    {
        public TransactionsCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override int GetConsumedGas(JsonElement result)
        {
            return (int)(((result.OptionalInt64("consumed_milligas") ?? 0) + 999) / 1000);
        }
    }
}
