using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    public class SetDelegateParametersOperation : ManagerOperation
    {
        public long? LimitOfStakingOverBaking { get; set; }
        public long? EdgeOfBakingOverStaking { get; set; }
        public int? ActivationCycle { get; set; }
    }

    public static class SetDelegateParametersOperationModel
    {
        public static void BuildSetDelegateParametersOperationModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<SetDelegateParametersOperation>()
                .HasKey(x => x.Id);
            #endregion
            
            #region props
            modelBuilder.Entity<SetDelegateParametersOperation>()
                .Property(x => x.OpHash)
                .IsFixedLength(true)
                .HasMaxLength(51)
                .IsRequired();
            #endregion

            #region indexes
            modelBuilder.Entity<SetDelegateParametersOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<SetDelegateParametersOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<SetDelegateParametersOperation>()
                .HasIndex(x => x.SenderId);
            #endregion

            #region relations
            modelBuilder.Entity<SetDelegateParametersOperation>()
                .HasOne(x => x.Block)
                .WithMany(x => x.SetDelegateParametersOps)
                .HasForeignKey(x => x.Level)
                .HasPrincipalKey(x => x.Level);
            #endregion
        }
    }
}
