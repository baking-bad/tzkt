using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class AutostakingOperation
    {
        public long Id { get; set; }
        public int Level { get; set; }
        public StakingAction Action { get; set; }

        public long Amount { get; set; }
        public int BakerId { get; set; }
        public int StakingUpdatesCount { get; set; }
    }

    public static class AutostakingOperationModel
    {
        public static void BuildAutostakingOperationModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<AutostakingOperation>()
                .HasKey(x => x.Id);
            #endregion

            #region indexes
            modelBuilder.Entity<AutostakingOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<AutostakingOperation>()
                .HasIndex(x => x.BakerId);
            #endregion
        }
    }
}
