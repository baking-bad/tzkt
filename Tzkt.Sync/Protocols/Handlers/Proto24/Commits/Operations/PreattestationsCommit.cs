using System.Text.Json;

namespace Tzkt.Sync.Protocols.Proto24
{
    class PreattestationsCommit(ProtocolHandler protocol) : Proto19.PreattestationsCommit(protocol)
    {
        protected override int GetPreattestedSlots(JsonElement metadata)
        {
            // TODO: remove it when block receipts are updated
            return metadata.Required("consensus_power").RequiredInt32("slots");
        }
    }
}
