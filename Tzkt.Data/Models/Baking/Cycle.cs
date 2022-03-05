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
        public long TotalStaking { get; set; }
        public long TotalDelegated { get; set; }
        public int TotalDelegators { get; set; }
        public int TotalBakers { get; set; }
        public int SelectedBakers { get; set; }
        public long SelectedStake { get; set; }

        public byte[] Seed { get; set; }
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