using System.Text.Json;

namespace Tzkt.Sync.Protocols.Proto24
{
    class PreattestationsCommit : Proto23.PreattestationsCommit
    {
        public PreattestationsCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override int GetPreattestedSlots(JsonElement metadata)
        {
            var consensus = metadata.Required("consensus_power");
            return consensus.RequiredInt32("slots");
        }
    }
}