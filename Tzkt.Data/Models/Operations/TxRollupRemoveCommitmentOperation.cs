using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    public class TxRollupRemoveCommitmentOperation : ManagerOperation
    {
        public int? RollupId { get; set; }
    }

    public static class TxRollupRemoveCommitmentOperationModel
    {
        public static void BuildTxRollupRemoveCommitmentOperationModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<TxRollupRemoveCommitmentOperation>()
                .HasKey(x => x.Id);
            #endregion
            
            #region props
            modelBuilder.Entity<TxRollupRemoveCommitmentOperation>()
                .Property(x => x.OpHash)
                .IsFixedLength(true)
                .HasMaxLength(51)
                .IsRequired();
            #endregion

            #region indexes
            modelBuilder.Entity<TxRollupRemoveCommitmentOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<TxRollupRemoveCommitmentOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<TxRollupRemoveCommitmentOperation>()
                .HasIndex(x => x.SenderId);

            modelBuilder.Entity<TxRollupRemoveCommitmentOperation>()
                .HasIndex(x => x.RollupId);
            #endregion

            #region relations
            modelBuilder.Entity<TxRollupRemoveCommitmentOperation>()
                .HasOne(x => x.Block)
                .WithMany(x => x.TxRollupRemoveCommitmentOps)
                .HasForeignKey(x => x.Level)
                .HasPrincipalKey(x => x.Level);
            #endregion
        }
    }
}
