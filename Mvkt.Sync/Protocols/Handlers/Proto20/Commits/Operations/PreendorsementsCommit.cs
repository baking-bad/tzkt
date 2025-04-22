using System.Text.Json;

namespace Mvkt.Sync.Protocols.Proto20
{
    class PreendorsementsCommit : Proto19.PreendorsementsCommit
    {
        public PreendorsementsCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override int GetPreendorsedSlots(JsonElement metadata)
        {
            return metadata.RequiredInt32("consensus_power");
        }
    }
}
