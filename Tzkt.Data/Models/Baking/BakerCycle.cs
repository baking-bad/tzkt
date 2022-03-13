using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class BakerCycle
    {
        public int Id { get; set; }
        public int Cycle { get; set; }
        public int BakerId { get; set; }

        public int DelegatorsCount { get; set; }
        public long DelegatedBalance { get; set; }
        public long StakingBalance { get; set; }
        public long ActiveStake { get; set; }
        public long SelectedStake { get; set; }

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
        public long BlockRewards { get; set; }
        public long MissedBlockRewards { get; set; }

        public long FutureEndorsementRewards { get; set; }
        public long EndorsementRewards { get; set; }
        public long MissedEndorsementRewards { get; set; }

        public long BlockFees { get; set; }
        public long MissedBlockFees { get; set; }

        public long DoubleBakingRewards { get; set; }
        public long DoubleBakingLosses { get; set; }

        public long DoubleEndorsingRewards { get; set; }
        public long DoubleEndorsingLosses { get; set; }

        public long DoublePreendorsingRewards { get; set; }
        public long DoublePreendorsingLosses { get; set; }

        public long RevelationRewards { get; set; }
        public long RevelationLosses { get; set; }
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
        }
    }
}
