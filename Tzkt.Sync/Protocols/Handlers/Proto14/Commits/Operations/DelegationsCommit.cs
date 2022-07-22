using System.Text.Json;

namespace Tzkt.Sync.Protocols.Proto14
{
    class DelegationsCommit : Proto1.DelegationsCommit
    {
        public DelegationsCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override int GetConsumedGas(JsonElement result)
        {
            return (int)(((result.OptionalInt64("consumed_milligas") ?? 0) + 999) / 1000);
        }
    }
}
