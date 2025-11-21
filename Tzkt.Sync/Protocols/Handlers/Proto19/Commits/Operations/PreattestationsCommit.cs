using System.Text.Json;

namespace Tzkt.Sync.Protocols.Proto19
{
    class PreattestationsCommit(ProtocolHandler protocol) : Proto12.PreattestationsCommit(protocol)
    {
        protected override long GetPower(JsonElement metadata)
        {
            return metadata.RequiredInt64("consensus_power");
        }
    }
}
