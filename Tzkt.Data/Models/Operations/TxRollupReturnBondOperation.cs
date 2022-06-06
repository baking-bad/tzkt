using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    public class TxRollupReturnBondOperation : ManagerOperation
    {
        public int? RollupId { get; set; }
        public long Bond { get; set; }
    }

    public static class TxRollupReturnBondOperationModel
    {
        public static void BuildTxRollupReturnBondOperationModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<TxRollupReturnBondOperation>()
                .HasKey(x => x.Id);
            #endregion
            
            #region props
            modelBuilder.Entity<TxRollupReturnBondOperation>()
                .Property(x => x.OpHash)
                .IsFixedLength(true)
                .HasMaxLength(51)
                .IsRequired();
            #endregion

            #region indexes
            modelBuilder.Entity<TxRollupReturnBondOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<TxRollupReturnBondOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<TxRollupReturnBondOperation>()
                .HasIndex(x => x.SenderId);

            modelBuilder.Entity<TxRollupReturnBondOperation>()
                .HasIndex(x => x.RollupId);
            #endregion

            #region relations
            modelBuilder.Entity<TxRollupReturnBondOperation>()
                .HasOne(x => x.Block)
                .WithMany(x => x.TxRollupReturnBondOps)
                .HasForeignKey(x => x.Level)
                .HasPrincipalKey(x => x.Level);
            #endregion
        }
    }
}
