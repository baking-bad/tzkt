using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class BakerCycle
    {
        public int Id { get; set; }
        public int Cycle { get; set; }
        public int BakerId { get; set; }

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
        #endregion

        #region rewards
        public long FutureBlockRewards { get; set; }
        public long MissedBlockRewards { get; set; }
        public long BlockRewardsLiquid { get; set; }
        public long BlockRewardsStakedOwn { get; set; }
        public long BlockRewardsStakedShared { get; set; }

        public long FutureEndorsementRewards { get; set; }
        public long MissedEndorsementRewards { get; set; }
        public long EndorsementRewardsLiquid { get; set; }
        public long EndorsementRewardsStakedOwn { get; set; }
        public long EndorsementRewardsStakedShared { get; set; }

        public long BlockFees { get; set; }
        public long MissedBlockFees { get; set; }

        public long DoubleBakingRewards { get; set; }
        public long DoubleBakingLossesOwn { get; set; }
        public long DoubleBakingLossesShared { get; set; }

        public long DoubleEndorsingRewards { get; set; }
        public long DoubleEndorsingLossesOwn { get; set; }
        public long DoubleEndorsingLossesShared { get; set; }

        public long DoublePreendorsingRewards { get; set; }
        public long DoublePreendorsingLossesOwn { get; set; }
        public long DoublePreendorsingLossesShared { get; set; }

        public long VdfRevelationRewardsLiquid { get; set; }
        public long VdfRevelationRewardsStakedOwn { get; set; }
        public long VdfRevelationRewardsStakedShared { get; set; }

        public long NonceRevelationRewardsLiquid { get; set; }
        public long NonceRevelationRewardsStakedOwn { get; set; }
        public long NonceRevelationRewardsStakedShared { get; set; }
        public long NonceRevelationLosses { get; set; }
        #endregion

        #region expected
        public double ExpectedBlocks { get; set; }
        public double ExpectedEndorsements { get; set; }
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
