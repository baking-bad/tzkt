using System.Text.Json;

namespace Tzkt.Sync.Protocols.Proto19
{
    class PreendorsementsCommit : Proto12.PreendorsementsCommit
    {
        public PreendorsementsCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override int GetPreendorsedSlots(JsonElement metadata)
        {
            return metadata.RequiredInt32("consensus_power");
        }
    }
}
