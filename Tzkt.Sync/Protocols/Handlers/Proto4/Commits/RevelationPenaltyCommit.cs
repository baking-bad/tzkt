using System.Text.Json;

namespace Tzkt.Sync.Protocols.Proto4
{
    class RevelationPenaltyCommit : Proto2.RevelationPenaltyCommit
    {
        public RevelationPenaltyCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override int GetFreezerCycle(JsonElement el) => el.RequiredInt32("cycle");
    }
}
