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

        public long BakingPower { get; set; }
        public long TotalBakingPower { get; set; }

        #region rights
        public int FutureBlocks { get; set; }
        public int Blocks { get; set; }
        public int MissedBlocks { get; set; }

        public int FutureEndorsements { get; set; }
        public int Endorsements { get; set; }
        public int MissedEndorsements { get; set; }

        // TODO: add dal rights
        #endregion

        #region rewards
        public long FutureBlockRewards { get; set; }
        public long MissedBlockRewards { get; set; }
        public long BlockRewardsDelegated { get; set; }
        public long BlockRewardsStakedOwn { get; set; }
        public long BlockRewardsStakedEdge { get; set; }
        public long BlockRewardsStakedShared { get; set; }

        public long FutureEndorsementRewards { get; set; }
        public long MissedEndorsementRewards { get; set; }
        public long EndorsementRewardsDelegated { get; set; }
        public long EndorsementRewardsStakedOwn { get; set; }
        public long EndorsementRewardsStakedEdge { get; set; }
        public long EndorsementRewardsStakedShared { get; set; }

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

        public long DoubleEndorsingRewards { get; set; }
        public long DoubleEndorsingLostStaked { get; set; }
        public long DoubleEndorsingLostUnstaked { get; set; }
        public long DoubleEndorsingLostExternalStaked { get; set; }
        public long DoubleEndorsingLostExternalUnstaked { get; set; }

        public long DoublePreendorsingRewards { get; set; }
        public long DoublePreendorsingLostStaked { get; set; }
        public long DoublePreendorsingLostUnstaked { get; set; }
        public long DoublePreendorsingLostExternalStaked { get; set; }
        public long DoublePreendorsingLostExternalUnstaked { get; set; }

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
        public double ExpectedEndorsements { get; set; }
        public long ExpectedDalShards { get; set; }
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
