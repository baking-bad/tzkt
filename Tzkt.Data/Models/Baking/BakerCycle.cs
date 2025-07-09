using System.Numerics;
using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class BakerCycle
    {
        public required int Id { get; set; }
        public required int Cycle { get; set; }
        public required int BakerId { get; set; }

        public long OwnDelegatedBalance { get; set; }
        public long ExternalDelegatedBalance { get; set; }
        public int DelegatorsCount { get; set; }

        public long OwnStakedBalance { get; set; }
        public long ExternalStakedBalance { get; set; }
        public int StakersCount { get; set; }

        public BigInteger? IssuedPseudotokens { get; set; }

        public long BakingPower { get; set; }
        public long TotalBakingPower { get; set; }

        #region rights
        public int FutureBlocks { get; set; }
        public int Blocks { get; set; }
        public int MissedBlocks { get; set; }

        public int FutureAttestations { get; set; }
        public int Attestations { get; set; }
        public int MissedAttestations { get; set; }

        // TODO: add dal rights
        //public int FutureDalAttestations { get; set; }
        //public int DalAttestations { get; set; }
        //public int MissedDalAttestations { get; set; }
        #endregion

        #region rewards
        public long FutureBlockRewards { get; set; }
        public long MissedBlockRewards { get; set; }
        public long BlockRewardsDelegated { get; set; }
        public long BlockRewardsStakedOwn { get; set; }
        public long BlockRewardsStakedEdge { get; set; }
        public long BlockRewardsStakedShared { get; set; }

        public long FutureAttestationRewards { get; set; }
        public long MissedAttestationRewards { get; set; }
        public long AttestationRewardsDelegated { get; set; }
        public long AttestationRewardsStakedOwn { get; set; }
        public long AttestationRewardsStakedEdge { get; set; }
        public long AttestationRewardsStakedShared { get; set; }

        public long FutureDalAttestationRewards { get; set; }
        public long MissedDalAttestationRewards { get; set; }
        public long DalAttestationRewardsDelegated { get; set; }
        public long DalAttestationRewardsStakedOwn { get; set; }
        public long DalAttestationRewardsStakedEdge { get; set; }
        public long DalAttestationRewardsStakedShared { get; set; }

        public long BlockFees { get; set; }
        public long MissedBlockFees { get; set; }

        public long DoubleBakingRewards { get; set; }
        public long DoubleBakingLostStaked { get; set; }
        public long DoubleBakingLostUnstaked { get; set; }
        public long DoubleBakingLostExternalStaked { get; set; }
        public long DoubleBakingLostExternalUnstaked { get; set; }

        public long DoubleConsensusRewards { get; set; }
        public long DoubleConsensusLostStaked { get; set; }
        public long DoubleConsensusLostUnstaked { get; set; }
        public long DoubleConsensusLostExternalStaked { get; set; }
        public long DoubleConsensusLostExternalUnstaked { get; set; }

        public long VdfRevelationRewardsDelegated { get; set; }
        public long VdfRevelationRewardsStakedOwn { get; set; }
        public long VdfRevelationRewardsStakedEdge { get; set; }
        public long VdfRevelationRewardsStakedShared { get; set; }

        public long NonceRevelationRewardsDelegated { get; set; }
        public long NonceRevelationRewardsStakedOwn { get; set; }
        public long NonceRevelationRewardsStakedEdge { get; set; }
        public long NonceRevelationRewardsStakedShared { get; set; }
        public long NonceRevelationLosses { get; set; }
        #endregion

        #region expected
        public double ExpectedBlocks { get; set; }
        public double ExpectedAttestations { get; set; }
        public long ExpectedDalAttestations { get; set; }
        #endregion
    }

    public static class BakerCycleModel
    {
        public static void BuildBakerCycleModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<BakerCycle>()
                .HasKey(x => x.Id);
            #endregion

            #region indexes
            modelBuilder.Entity<BakerCycle>()
                .HasIndex(x => new { x.Cycle, x.BakerId });

            modelBuilder.Entity<BakerCycle>()
                .HasIndex(x => x.BakerId);
            #endregion
        }
    }
}
