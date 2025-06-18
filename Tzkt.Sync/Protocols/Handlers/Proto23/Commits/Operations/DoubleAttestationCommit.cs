using System.Text.Json;

namespace Tzkt.Sync.Protocols.Proto23
{
    class DoubleAttestationCommit(ProtocolHandler protocol) : Proto19.DoubleAttestationCommit(protocol)
    {
        protected override int GetAccusedLevel(JsonElement content)
        {
            return content.Required("metadata").Required("misbehaviour").RequiredInt32("level");
        }

        protected override string GetOffender(JsonElement content)
        {
            return content.Required("metadata").RequiredString("punished_delegate");
        }
    }
}
