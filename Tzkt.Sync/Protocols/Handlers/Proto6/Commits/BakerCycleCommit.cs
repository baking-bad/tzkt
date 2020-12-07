using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto6
{
    class BakerCycleCommit : Proto5.BakerCycleCommit
    {
        public BakerCycleCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override long GetFutureBlockReward(Protocol protocol, int cycle)
            => protocol.BlockReward0 == 0 && cycle < protocol.PreservedCycles + 2 ? 0 : 40_000_000L; //TODO: use protocol_parameters

        protected override long GetBlockReward(Protocol protocol, int priority, int slots)
            => (priority == 0 ? protocol.BlockReward0 : protocol.BlockReward1) * slots;

        protected override long GetFutureEndorsementReward(Protocol protocol, int cycle, int slots)
            => protocol.EndorsementReward0 == 0 && cycle < protocol.PreservedCycles + 2 ? 0 : slots * 1_250_000L; //TODO: use protocol_parameters

        protected override long GetEndorsementReward(Protocol protocol, int slots, int priority)
            => (priority == 0 ? protocol.EndorsementReward0 : protocol.EndorsementReward1) * slots;
    }
}
