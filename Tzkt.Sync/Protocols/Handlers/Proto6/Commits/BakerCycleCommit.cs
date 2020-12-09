using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto6
{
    class BakerCycleCommit : Proto5.BakerCycleCommit
    {
        public BakerCycleCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override long GetFutureBlockReward(Protocol protocol, int cycle)
            => cycle < protocol.NoRewardCycles ? 0 : (protocol.BlockReward0 * protocol.EndorsersPerBlock);

        protected override long GetBlockReward(Protocol protocol, int cycle, int priority, int slots)
            => cycle < protocol.NoRewardCycles ? 0 : ((priority == 0 ? protocol.BlockReward0 : protocol.BlockReward1) * slots);

        protected override long GetFutureEndorsementReward(Protocol protocol, int cycle, int slots)
            => cycle < protocol.NoRewardCycles ? 0 : (slots * protocol.EndorsementReward0);

        protected override long GetEndorsementReward(Protocol protocol, int cycle, int slots, int priority)
            => cycle < protocol.NoRewardCycles ? 0 : ((priority == 0 ? protocol.EndorsementReward0 : protocol.EndorsementReward1) * slots);
    }
}
