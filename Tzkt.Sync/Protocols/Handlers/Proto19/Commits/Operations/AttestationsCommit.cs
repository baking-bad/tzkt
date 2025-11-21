using System.Text.Json;

namespace Tzkt.Sync.Protocols.Proto19
{
    class AttestationsCommit(ProtocolHandler protocol) : Proto12.AttestationsCommit(protocol)
    {
        protected override long GetPower(JsonElement metadata)
        {
            return metadata.RequiredInt64("consensus_power");
        }
    }
}
