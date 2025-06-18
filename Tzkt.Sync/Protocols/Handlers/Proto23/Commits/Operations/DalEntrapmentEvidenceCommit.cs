using System.Text.Json;

namespace Tzkt.Sync.Protocols.Proto23
{
    class DalEntrapmentEvidenceCommit(ProtocolHandler protocol) : Proto22.DalEntrapmentEvidenceCommit(protocol)
    {
        protected override int GetTrapLevel(JsonElement content)
        {
            var op = content.Required("attestation").Required("operations");
            if (!op.TryGetProperty("consensus_content", out var consensusContent))
                consensusContent = op;

            return consensusContent.RequiredInt32("level");
        }

        protected override int GetConsensusSlot(JsonElement content)
        {
            return content.RequiredInt32("consensus_slot");
        }
    }
}
