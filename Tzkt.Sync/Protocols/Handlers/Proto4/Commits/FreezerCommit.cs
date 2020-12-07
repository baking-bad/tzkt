using System.Text.Json;

namespace Tzkt.Sync.Protocols.Proto4
{
    class FreezerCommit : Proto1.FreezerCommit
    {
        public FreezerCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override int GetFreezerCycle(JsonElement el) => el.RequiredInt32("cycle");
    }
}
