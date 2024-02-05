using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class DelegatorCycle
    {
        public int Id { get; set; }
        public int Cycle { get; set; }
        public int DelegatorId { get; set; }

        public int BakerId { get; set; }
        public long DelegatedBalance { get; set; }
        public long StakedBalance { get; set; }
    }

    public static class DelegatorCycleModel
    {
        public static void BuildDelegatorCycleModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<DelegatorCycle>()
                .HasKey(x => x.Id);
            #endregion

            #region indexes
            modelBuilder.Entity<DelegatorCycle>()
                .HasIndex(x => new { x.Cycle, x.BakerId });

            modelBuilder.Entity<DelegatorCycle>()
                .HasIndex(x => new { x.Cycle, x.DelegatorId });

            modelBuilder.Entity<DelegatorCycle>()
                .HasIndex(x => x.DelegatorId);
            #endregion
        }
    }
}
