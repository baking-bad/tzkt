using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto19
{
    class DoubleAttestationCommit : Proto18.DoubleAttestationCommit
    {
        public DoubleAttestationCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override int GetSlashingLevel(Block block, Protocol protocol, int accusedLevel)
        {
            return Cache.Protocols.GetCycleEnd(protocol.GetCycle(accusedLevel) + protocol.SlashingDelay);
        }
    }
}
