using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    public class StakingOperation : ManagerOperation
    {
        public int StakerId { get; set; }
        public StakingAction Action { get; set; }
        public long RequestedAmount { get; set; }

        public long? Amount { get; set; }
        public int? BakerId { get; set; }
        public int? StakingUpdatesCount { get; set; }
    }

    public enum StakingAction
    {
        Stake,
        Unstake,
        Finalize
    }

    public static class StakingOperationModel
    {
        public static void BuildStakingOperationModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<StakingOperation>()
                .HasKey(x => x.Id);
            #endregion
            
            #region props
            modelBuilder.Entity<StakingOperation>()
                .Property(x => x.OpHash)
                .IsFixedLength(true)
                .HasMaxLength(51)
                .IsRequired();
            #endregion

            #region indexes
            modelBuilder.Entity<StakingOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<StakingOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<StakingOperation>()
                .HasIndex(x => x.SenderId);

            modelBuilder.Entity<StakingOperation>()
                .HasIndex(x => x.StakerId);

            modelBuilder.Entity<StakingOperation>()
                .HasIndex(x => x.BakerId);
            #endregion
        }
    }
}
