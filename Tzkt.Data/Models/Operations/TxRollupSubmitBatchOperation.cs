using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    public class TxRollupSubmitBatchOperation : ManagerOperation
    {
        public int? RollupId { get; set; }
    }

    public static class TxRollupSubmitBatchOperationModel
    {
        public static void BuildTxRollupSubmitBatchOperationModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<TxRollupSubmitBatchOperation>()
                .HasKey(x => x.Id);
            #endregion
            
            #region props
            modelBuilder.Entity<TxRollupSubmitBatchOperation>()
                .Property(x => x.OpHash)
                .IsFixedLength(true)
                .HasMaxLength(51)
                .IsRequired();
            #endregion

            #region indexes
            modelBuilder.Entity<TxRollupSubmitBatchOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<TxRollupSubmitBatchOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<TxRollupSubmitBatchOperation>()
                .HasIndex(x => x.SenderId);

            modelBuilder.Entity<TxRollupSubmitBatchOperation>()
                .HasIndex(x => x.RollupId);
            #endregion

            #region relations
            modelBuilder.Entity<TxRollupSubmitBatchOperation>()
                .HasOne(x => x.Block)
                .WithMany(x => x.TxRollupSubmitBatchOps)
                .HasForeignKey(x => x.Level)
                .HasPrincipalKey(x => x.Level);
            #endregion
        }
    }
}
