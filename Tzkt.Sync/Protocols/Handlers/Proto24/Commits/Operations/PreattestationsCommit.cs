using System.Text.Json;

namespace Tzkt.Sync.Protocols.Proto24
{
    class PreattestationsCommit(ProtocolHandler protocol) : Proto19.PreattestationsCommit(protocol)
    {
        protected override long GetPower(JsonElement metadata)
        {
            var consensusPower = metadata.Required("consensus_power");
            return consensusPower.OptionalInt64("baking_power") ?? consensusPower.RequiredInt64("slots");
        }
    }
}
