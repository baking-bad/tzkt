using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto6
{
    class BakerCycleCommit(ProtocolHandler protocol) : Proto5.BakerCycleCommit(protocol)
    {
        protected override long GetFutureBlockReward(Protocol protocol, int cycle)
            => cycle < protocol.NoRewardCycles ? 0 : (protocol.BlockReward0 * protocol.AttestersPerBlock);

        protected override long GetBlockReward(Protocol protocol, int cycle, int priority, long slots)
            => cycle < protocol.NoRewardCycles ? 0 : ((priority == 0 ? protocol.BlockReward0 : protocol.BlockReward1) * slots);

        protected override long GetFutureAttestationReward(Protocol protocol, int cycle, long slots)
            => cycle < protocol.NoRewardCycles ? 0 : (slots * protocol.AttestationReward0);

        protected override long GetAttestationReward(Protocol protocol, int cycle, int slots, int priority)
            => cycle < protocol.NoRewardCycles ? 0 : ((priority == 0 ? protocol.AttestationReward0 : protocol.AttestationReward1) * slots);
    }
}
