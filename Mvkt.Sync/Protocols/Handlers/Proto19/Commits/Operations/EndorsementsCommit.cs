using System.Text.Json;

namespace Mvkt.Sync.Protocols.Proto19
{
    class EndorsementsCommit : Proto12.EndorsementsCommit
    {
        public EndorsementsCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override int GetEndorsedSlots(JsonElement metadata)
        {
            return metadata.RequiredInt32("consensus_power");
        }
    }
}
