using System.Numerics;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto18
{
    class VotingCommit : Proto13.VotingCommit
    {
        public VotingCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override long GetVotingPower(Data.Models.Delegate baker, Block block, Protocol protocol)
        {
            if (!Cache.AppState.Get().AIActivated || block.Cycle < Cache.AppState.Get().AIActivationCycle)
                return base.GetVotingPower(baker, block, protocol);

            return baker.TotalStakedBalance + (baker.StakingBalance - baker.TotalStakedBalance) / 2;
        }

        protected override bool BakerIsListed(Data.Models.Delegate baker, Block block, Protocol protocol)
        {
            if (!Cache.AppState.Get().AIActivated || block.Cycle < Cache.AppState.Get().AIActivationCycle)
                return base.BakerIsListed(baker, block, protocol);

            if (!baker.Staked)
                return false;

            var stakingOverBaking = Math.Min(protocol.MaxExternalOverOwnStakeRatio * 1_000_000, baker.LimitOfStakingOverBaking ?? long.MaxValue);

            var frozen = Math.Min(baker.TotalStakedBalance, baker.StakedBalance + (long)((BigInteger)baker.StakedBalance * stakingOverBaking / 1_000_000));
            var delegated = Math.Min(baker.StakingBalance - frozen, baker.StakedBalance * protocol.MaxDelegatedOverFrozenRatio);

            return frozen >= protocol.MinimalFrozenStake && frozen + delegated >= protocol.MinimalStake;
        }
    }
}
