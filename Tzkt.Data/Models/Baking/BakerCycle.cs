using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class BakerCycle
    {
        public int Id { get; set; }
        public int Cycle { get; set; }
        public int BakerId { get; set; }

        public int Rolls { get; set; }
        public long StakingBalance { get; set; }
        public long DelegatedBalance { get; set; }
        public int DelegatorsCount { get; set; }

        #region rights
        public int FutureBlocks { get; set; }
        public int OwnBlocks { get; set; }
        public int ExtraBlocks { get; set; }
        public int MissedOwnBlocks { get; set; }
        public int MissedExtraBlocks { get; set; }
        public int UncoveredOwnBlocks { get; set; }
        public int UncoveredExtraBlocks { get; set; }

        public int FutureEndorsements { get; set; }
        public int Endorsements { get; set; }
        public int MissedEndorsements { get; set; }
        public int UncoveredEndorsements { get; set; }
        #endregion

        #region rewards
        public long FutureBlockRewards { get; set; }
        public long OwnBlockRewards { get; set; }
        public long ExtraBlockRewards { get; set; }
        public long MissedOwnBlockRewards { get; set; }
        public long MissedExtraBlockRewards { get; set; }
        public long UncoveredOwnBlockRewards { get; set; }
        public long UncoveredExtraBlockRewards { get; set; }

        public long FutureEndorsementRewards { get; set; }
        public long EndorsementRewards { get; set; }
        public long MissedEndorsementRewards { get; set; }
        public long UncoveredEndorsementRewards { get; set; }

        public long OwnBlockFees { get; set; }
        public long ExtraBlockFees { get; set; }
        public long MissedOwnBlockFees { get; set; }
        public long MissedExtraBlockFees { get; set; }
        public long UncoveredOwnBlockFees { get; set; }
        public long UncoveredExtraBlockFees { get; set; }

        public long DoubleBakingRewards { get; set; }
        public long DoubleBakingLostDeposits { get; set; }
        public long DoubleBakingLostRewards { get; set; }
        public long DoubleBakingLostFees { get; set; }

        public long DoubleEndorsingRewards { get; set; }
        public long DoubleEndorsingLostDeposits { get; set; }
        public long DoubleEndorsingLostRewards { get; set; }
        public long DoubleEndorsingLostFees { get; set; }

        public long RevelationRewards { get; set; }
        public long RevelationLostRewards { get; set; }
        public long RevelationLostFees { get; set; }
        #endregion

        #region deposits
        public long FutureBlockDeposits { get; set; }
        public long BlockDeposits { get; set; }

        public long FutureEndorsementDeposits { get; set; }
        public long EndorsementDeposits { get; set; }
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
            #region indexes
            modelBuilder.Entity<BakerCycle>()
                .HasIndex(x => x.Id)
                .IsUnique(true);

            modelBuilder.Entity<BakerCycle>()
                .HasIndex(x => x.Cycle);

            modelBuilder.Entity<BakerCycle>()
                .HasIndex(x => x.BakerId);

            modelBuilder.Entity<BakerCycle>()
                .HasIndex(x => new { x.Cycle, x.BakerId })
                .IsUnique(true);
            #endregion

            #region keys
            modelBuilder.Entity<BakerCycle>()
                .HasKey(x => x.Id);
            #endregion
        }
    }
}
