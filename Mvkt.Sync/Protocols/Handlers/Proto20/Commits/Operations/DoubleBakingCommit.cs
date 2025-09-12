using Mvkt.Data.Models;

namespace Mvkt.Sync.Protocols.Proto20
{
    class DoubleBakingCommit : Proto19.DoubleBakingCommit
    {
        public DoubleBakingCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override int GetSlashingLevel(Block block, Protocol protocol, int accusedLevel)
        {
            return Cache.Protocols.GetCycleEnd(protocol.GetCycle(accusedLevel) + protocol.MaxSlashingPeriod - 1);
        }
    }
}
