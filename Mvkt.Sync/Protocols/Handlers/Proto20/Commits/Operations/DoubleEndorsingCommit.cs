using Mvkt.Data.Models;

namespace Mvkt.Sync.Protocols.Proto20
{
    class DoubleEndorsingCommit : Proto19.DoubleEndorsingCommit
    {
        public DoubleEndorsingCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override int GetSlashingLevel(Block block, Protocol protocol, int accusedLevel)
        {
            return Cache.Protocols.GetCycleEnd(protocol.GetCycle(accusedLevel) + protocol.MaxSlashingPeriod - 1);
        }
    }
}
