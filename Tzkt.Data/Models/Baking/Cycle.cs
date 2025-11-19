using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class Cycle
    {
        public required int Id { get; set; }
        public required int Index { get; set; }
        public required int FirstLevel { get; set; }
        public required int LastLevel { get; set; }
        public required byte[] Seed { get; set; }

        public int SnapshotLevel { get; set; }
        public int TotalBakers { get; set; }
        public long TotalBakingPower { get; set; }

        public long BlockReward { get; set; }
        public long BlockBonusPerBlock { get; set; }
        public long AttestationRewardPerBlock { get; set; }
        public long NonceRevelationReward { get; set; }
        public long VdfRevelationReward { get; set; }
        public long DalAttestationRewardPerShard{ get; set; }
    }

    public static class CycleModel
    {
        public static void BuildCycleModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<Cycle>()
                .HasKey(x => x.Id);
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