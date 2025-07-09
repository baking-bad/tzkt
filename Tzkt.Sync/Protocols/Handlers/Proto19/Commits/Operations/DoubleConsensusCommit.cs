using System.Text.Json;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto19
{
    class DoubleConsensusCommit(ProtocolHandler protocol) : Proto18.DoubleConsensusCommit(protocol)
    {
        protected override int GetSlashingLevel(Block block, Protocol protocol, int accusedLevel)
        {
            return Cache.Protocols.GetCycleEnd(protocol.GetCycle(accusedLevel) + protocol.SlashingDelay);
        }

        protected override DoubleConsensusKind GetKind(JsonElement content)
        {
            return content.RequiredString("kind") == "double_attestation_evidence"
                ? DoubleConsensusKind.DoubleAttestation
                : DoubleConsensusKind.DoublePreattestation;
        }
    }
}
