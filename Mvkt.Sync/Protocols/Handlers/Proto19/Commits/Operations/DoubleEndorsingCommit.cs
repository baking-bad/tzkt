using Mvkt.Data.Models;

namespace Mvkt.Sync.Protocols.Proto19
{
    class DoubleEndorsingCommit : Proto18.DoubleEndorsingCommit
    {
        public DoubleEndorsingCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override int GetSlashingLevel(Block block, Protocol protocol, int accusedLevel)
        {
            return Cache.Protocols.GetCycleEnd(protocol.GetCycle(accusedLevel) + protocol.MaxSlashingPeriod - 1);
        }
    }
}
