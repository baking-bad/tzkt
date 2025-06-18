using System.Text.Json;

namespace Tzkt.Sync.Protocols.Proto19
{
    class PreattestationsCommit : Proto12.PreattestationsCommit
    {
        public PreattestationsCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override int GetPreattestedSlots(JsonElement metadata)
        {
            return metadata.RequiredInt32("consensus_power");
        }
    }
}
