using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    public class TxRollupOriginationOperation : ManagerOperation
    {
        public int? RollupId { get; set; }
    }

    public static class TxRollupOriginationOperationModel
    {
        public static void BuildTxRollupOriginationOperationModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<TxRollupOriginationOperation>()
                .HasKey(x => x.Id);
            #endregion
            
            #region props
            modelBuilder.Entity<TxRollupOriginationOperation>()
                .Property(x => x.OpHash)
                .IsFixedLength(true)
                .HasMaxLength(51)
                .IsRequired();
            #endregion

            #region indexes
            modelBuilder.Entity<TxRollupOriginationOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<TxRollupOriginationOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<TxRollupOriginationOperation>()
                .HasIndex(x => x.SenderId);

            modelBuilder.Entity<TxRollupOriginationOperation>()
                .HasIndex(x => x.RollupId);
            #endregion

            #region relations
            modelBuilder.Entity<TxRollupOriginationOperation>()
                .HasOne(x => x.Block)
                .WithMany(x => x.TxRollupOriginationOps)
                .HasForeignKey(x => x.Level)
                .HasPrincipalKey(x => x.Level);
            #endregion
        }
    }
}
