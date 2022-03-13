using System.Text.Json;

namespace Tzkt.Sync.Protocols.Proto12
{
    class ActivationsCommit : Proto5.ActivationsCommit
    {
        public ActivationsCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override long ParseBalance(JsonElement balanceUpdates) => balanceUpdates[1].RequiredInt64("change");
    }
}
