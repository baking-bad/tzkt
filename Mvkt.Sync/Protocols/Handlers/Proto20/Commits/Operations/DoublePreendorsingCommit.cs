using Mvkt.Data.Models;

namespace Mvkt.Sync.Protocols.Proto20
{
    class DoublePreendorsingCommit : Proto19.DoublePreendorsingCommit
    {
        public DoublePreendorsingCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override int GetSlashingLevel(Block block, Protocol protocol, int accusedLevel)
        {
            return Cache.Protocols.GetCycleEnd(protocol.GetCycle(accusedLevel) + protocol.MaxSlashingPeriod - 1);
        }
    }
}
