using System.Text.Json;

namespace Tzkt.Sync.Protocols.Proto19
{
    class AttestationsCommit : Proto12.AttestationsCommit
    {
        public AttestationsCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override int GetAttestedSlots(JsonElement metadata)
        {
            return metadata.RequiredInt32("consensus_power");
        }
    }
}
