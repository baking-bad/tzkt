using System.Text.Json;

namespace Tzkt.Sync.Protocols.Proto24
{
    class AttestationsCommit : Proto23.AttestationsCommit
    {
        public AttestationsCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override int GetAttestedSlots(JsonElement metadata)
        {
            var consensus = metadata.Required("consensus_power");
            return consensus.RequiredInt32("slots");
        }
    }
}