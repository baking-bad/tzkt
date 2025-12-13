using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class Statistics
    {
        public required int Id { get; set; }
        public required int Level { get; set; }
        public int? Cycle { get; set; }
        public DateTime? Date { get; set; }

        #region supply
        public long TotalBootstrapped { get; set; }
        public long TotalCommitments { get; set; }

        public long TotalActivated { get; set; }
        public long TotalCreated { get; set; }
        public long TotalBurned { get; set; }
        public long TotalBanished { get; set; }
        public long TotalLost { get; set; }

        public long TotalFrozen { get; set; }
        public long TotalRollupBonds { get; set; }
        public long TotalSmartRollupBonds { get; set; }
        #endregion

        #region staking
        public long TotalOwnStaked { get; set; }
        public long TotalOwnDelegated { get; set; }
        public long TotalExternalStaked { get; set; }
        public long TotalExternalDelegated { get; set; }

        public long TotalBakingPower { get; set; }
        public long TotalVotingPower { get; set; }

        public int TotalBakers { get; set; }
        public int TotalStakers { get; set; }
        public int TotalDelegators { get; set; }
        #endregion
    }

    public static class StatisticsModel
    {
        public static void BuildStatisticsModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<Statistics>()
                .HasKey(x => x.Id);
            #endregion

            #region indexes
            modelBuilder.Entity<Statistics>()
                .HasIndex(x => x.Level)
                .IsUnique();

            modelBuilder.Entity<Statistics>()
                .HasIndex(x => x.Cycle)
                .HasFilter($@"""{nameof(Statistics.Cycle)}"" IS NOT NULL")
                .IsUnique();

            modelBuilder.Entity<Statistics>()
                .HasIndex(x => x.Date)
                .HasFilter($@"""{nameof(Statistics.Date)}"" IS NOT NULL")
                .IsUnique();
            #endregion
        }
    }
}
