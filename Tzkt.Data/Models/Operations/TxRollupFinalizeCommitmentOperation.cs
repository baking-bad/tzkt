using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    public class TxRollupFinalizeCommitmentOperation : ManagerOperation
    {
        public int? RollupId { get; set; }
    }

    public static class TxRollupFinalizeCommitmentOperationModel
    {
        public static void BuildTxRollupFinalizeCommitmentOperationModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<TxRollupFinalizeCommitmentOperation>()
                .HasKey(x => x.Id);
            #endregion
            
            #region props
            modelBuilder.Entity<TxRollupFinalizeCommitmentOperation>()
                .Property(x => x.OpHash)
                .IsFixedLength(true)
                .HasMaxLength(51)
                .IsRequired();
            #endregion

            #region indexes
            modelBuilder.Entity<TxRollupFinalizeCommitmentOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<TxRollupFinalizeCommitmentOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<TxRollupFinalizeCommitmentOperation>()
                .HasIndex(x => x.SenderId);

            modelBuilder.Entity<TxRollupFinalizeCommitmentOperation>()
                .HasIndex(x => x.RollupId);
            #endregion

            #region relations
            modelBuilder.Entity<TxRollupFinalizeCommitmentOperation>()
                .HasOne(x => x.Block)
                .WithMany(x => x.TxRollupFinalizeCommitmentOps)
                .HasForeignKey(x => x.Level)
                .HasPrincipalKey(x => x.Level);
            #endregion
        }
    }
}
