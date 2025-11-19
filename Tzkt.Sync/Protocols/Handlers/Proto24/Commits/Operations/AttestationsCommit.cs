using System.Text.Json;

namespace Tzkt.Sync.Protocols.Proto24
{
    class AttestationsCommit(ProtocolHandler protocol) : Proto19.AttestationsCommit(protocol)
    {
        protected override int GetAttestedSlots(JsonElement metadata)
        {
            // TODO: remove it when block receipts are updated
            return metadata.Required("consensus_power").RequiredInt32("slots");
        }
    }
}
