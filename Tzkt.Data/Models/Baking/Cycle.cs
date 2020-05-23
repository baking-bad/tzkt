using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class Cycle
    {
        public int Id { get; set; }
        public int Index { get; set; }
        public int SnapshotIndex { get; set; }
        public int SnapshotLevel { get; set; }
        public int TotalRolls { get; set; }
        public long TotalStaking { get; set; }
        public long TotalDelegated { get; set; }
        public int TotalDelegators { get; set; }
        public int TotalBakers { get; set; }

        public string Seed { get; set; }
    }

    public static class CycleModel
    {
        public static void BuildCycleModel(this ModelBuilder modelBuilder)
        {
            #region indexes
            modelBuilder.Entity<Cycle>()
                .HasIndex(x => x.Index)
                .IsUnique();
            #endregion

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
                .HasMaxLength(64)
                .IsRequired();
            #endregion
        }
    }
}