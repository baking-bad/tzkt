using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto13
{
    class VotingCommit : Proto8.VotingCommit
    {
        public VotingCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override long GetVotingPower(Data.Models.Delegate baker, Protocol protocol)
        {
            return baker.StakingBalance;
        }
    }
}
