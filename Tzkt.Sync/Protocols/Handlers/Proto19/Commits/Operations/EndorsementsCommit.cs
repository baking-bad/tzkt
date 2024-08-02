using System.Numerics;
using System.Text.Json;

namespace Tzkt.Sync.Protocols.Proto19
{
    class EndorsementsCommit : Proto12.EndorsementsCommit
    {
        public EndorsementsCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override int GetEndorsedSlots(JsonElement metadata)
        {
            return metadata.RequiredInt32("consensus_power");
        }

        protected override BigInteger? GetDalAttestation(JsonElement content) {
            return content.OptionalBigInteger("dal_attestation");
        }
    }
}
