using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class Cycle
    {
        public int Id { get; set; }
        public int Index { get; set; }
        public int FirstLevel { get; set; }
        public int LastLevel { get; set; }
        public int SnapshotIndex { get; set; }
        public int SnapshotLevel { get; set; }

        public int TotalBakers { get; set; }
        public long TotalBakingPower { get; set; }

        public byte[] Seed { get; set; }

        public long BlockReward { get; set; }
        public long BlockBonusPerSlot { get; set; }
        public long MaxBlockReward { get; set; }
        public long EndorsementRewardPerSlot { get; set; }
        public long NonceRevelationReward { get; set; }
        public long VdfRevelationReward { get; set; }
        public long LBSubsidy { get; set; }
    }

    public static class CycleModel
    {
        public static void BuildCycleModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<Cycle>()
                .HasKey(x => x.Id);

            modelBuilder.Entity<Cycle>()
                .HasAlternateKey(x => x.Index);
            #endregion

            #region props
            modelBuilder.Entity<Cycle>()
                .Property(x => x.Seed)
                .IsFixedLength(true)
                .HasMaxLength(32)
                .IsRequired();
            #endregion

            #region indexes
            modelBuilder.Entity<Cycle>()
                .HasIndex(x => x.Index)
                .IsUnique();
            #endregion
        }
    }
}