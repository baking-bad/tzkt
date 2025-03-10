using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    public class TxRollupCommitOperation : ManagerOperation
    {
        public int? RollupId { get; set; }
        public long Bond { get; set; }
    }

    public static class TxRollupCommitOperationModel
    {
        public static void BuildTxRollupCommitOperationModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<TxRollupCommitOperation>()
                .HasKey(x => x.Id);
            #endregion
            
            #region props
            modelBuilder.Entity<TxRollupCommitOperation>()
                .Property(x => x.OpHash)
                .IsFixedLength(true)
                .HasMaxLength(51)
                .IsRequired();
            #endregion

            #region indexes
            modelBuilder.Entity<TxRollupCommitOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<TxRollupCommitOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<TxRollupCommitOperation>()
                .HasIndex(x => x.SenderId);

            modelBuilder.Entity<TxRollupCommitOperation>()
                .HasIndex(x => x.RollupId);
            #endregion
        }
    }
}
