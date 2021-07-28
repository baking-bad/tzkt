using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class DelegatorCycle
    {
        public int Id { get; set; }
        public int Cycle { get; set; }
        public int DelegatorId { get; set; }

        public int BakerId { get; set; }
        public long Balance { get; set; }
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
                .HasIndex(x => x.Cycle);

            modelBuilder.Entity<DelegatorCycle>()
                .HasIndex(x => x.DelegatorId);

            modelBuilder.Entity<DelegatorCycle>()
                .HasIndex(x => new { x.Cycle, x.DelegatorId })
                .IsUnique(true);

            modelBuilder.Entity<DelegatorCycle>()
                .HasIndex(x => new { x.Cycle, x.BakerId });
            #endregion
        }
    }
}
