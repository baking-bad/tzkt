using Microsoft.EntityFrameworkCore;

namespace Mvkt.Data.Models
{
    public class AutostakingOperation
    {
        public long Id { get; set; }
        public int Level { get; set; }
        public int BakerId { get; set; }
        public AutostakingAction Action { get; set; }
        public int Cycle { get; set; }
        public long Amount { get; set; }
    }

    public enum AutostakingAction
    {
        Stake,
        Unstake,
        Finalize,
        Restake
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
