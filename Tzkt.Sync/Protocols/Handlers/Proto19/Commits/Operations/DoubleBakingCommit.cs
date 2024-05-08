using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto19
{
    class DoubleBakingCommit : Proto18.DoubleBakingCommit
    {
        public DoubleBakingCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override int GetSlashingLevel(Block block, Protocol protocol, int accusedLevel)
        {
            return protocol.GetCycleEnd(protocol.GetCycle(accusedLevel) + protocol.MaxSlashingPeriod - 1);
        }
    }
}
