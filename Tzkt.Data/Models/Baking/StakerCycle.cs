using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class StakerCycle
    {
        public required int Id { get; set; }
        public required int Cycle { get; set; }
        public required int StakerId { get; set; }
        public required int BakerId { get; set; }
        public long EdgeOfBakingOverStaking { get; set; }
        public long InitialStake { get; set; }
        public long AvgStake { get; set; }
        public long AddedStake { get; set; }
        public long RemovedStake { get; set; }
        public long? FinalStake { get; set; }
    }

    public static class StakerCycleModel
    {
        public static void BuildStakerCycleModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<StakerCycle>()
                .HasKey(x => x.Id);
            #endregion

            #region indexes
            modelBuilder.Entity<StakerCycle>()
                .HasIndex(x => new { x.StakerId, x.Cycle });

            modelBuilder.Entity<StakerCycle>()
                .HasIndex(x => new { x.Cycle, x.BakerId });
            #endregion
        }
    }
}
