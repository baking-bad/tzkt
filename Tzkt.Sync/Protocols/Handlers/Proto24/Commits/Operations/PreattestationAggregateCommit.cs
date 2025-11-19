using System.Text.Json;

namespace Tzkt.Sync.Protocols.Proto24
{
    class PreattestationAggregateCommit(ProtocolHandler protocol) : Proto23.PreattestationAggregateCommit(protocol)
    {
        protected override int GetAttestedSlots(JsonElement c)
        {
            // TODO: remove it when block receipts are updated
            return c.Required("consensus_power").RequiredInt32("slots");
        }
    }
}
