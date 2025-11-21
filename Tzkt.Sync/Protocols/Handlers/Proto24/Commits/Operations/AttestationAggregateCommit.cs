using System.Text.Json;

namespace Tzkt.Sync.Protocols.Proto24
{
    class AttestationAggregateCommit(ProtocolHandler protocol) : Proto23.AttestationAggregateCommit(protocol)
    {
        protected override long GetPower(JsonElement c)
        {
            var consensusPower = c.Required("consensus_power");
            return consensusPower.OptionalInt64("baking_power") ?? consensusPower.RequiredInt64("slots");
        }
    }
}
