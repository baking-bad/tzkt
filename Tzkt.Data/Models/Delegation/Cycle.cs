using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class Cycle
    {
        public int Id { get; set; }
        public int Index { get; set; }

        #region shapshot
        public int Snapshot { get; set; }
        public int ActiveBakers { get; set; }
        public int ActiveDelegators { get; set; }
        public int TotalRolls { get; set; }
        public long TotalBalances { get; set; }
        #endregion

        #region stats
        #endregion
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
            #endregion
        }
    }
}
