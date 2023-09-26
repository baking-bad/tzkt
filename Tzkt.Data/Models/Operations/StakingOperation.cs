using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    public class StakingOperation : ManagerOperation
    {
        public StakingOperationKind Kind { get; set; }

        public int? BakerId { get; set; }
        public long? Amount { get; set; }
        public long? Pseudotokens { get; set; }

        public long? PrevStakedBalance { get; set; }

        public int? FirstCycleUnstaked { get; set; }
        public int? LastCycleUnstaked { get; set; }

        public long? LimitOfStakingOverBaking { get; set; }
        public long? EdgeOfBakingOverStaking { get; set; }
        public int? ActivationCycle { get; set; }
    }

    public enum StakingOperationKind
    {
        Stake,
        Unstake,
        FinalizeUnstake,
        SetDelegateParameter
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
                .HasIndex(x => x.BakerId);
            #endregion

            #region relations
            modelBuilder.Entity<StakingOperation>()
                .HasOne(x => x.Block)
                .WithMany(x => x.StakingOps)
                .HasForeignKey(x => x.Level)
                .HasPrincipalKey(x => x.Level);
            #endregion
        }
    }
}
