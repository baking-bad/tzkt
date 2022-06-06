using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    public class TxRollupDispatchTicketsOperation : ManagerOperation
    {
        public int? RollupId { get; set; }
    }

    public static class TxRollupDispatchTicketsOperationModel
    {
        public static void BuildTxRollupDispatchTicketsOperationModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<TxRollupDispatchTicketsOperation>()
                .HasKey(x => x.Id);
            #endregion
            
            #region props
            modelBuilder.Entity<TxRollupDispatchTicketsOperation>()
                .Property(x => x.OpHash)
                .IsFixedLength(true)
                .HasMaxLength(51)
                .IsRequired();
            #endregion

            #region indexes
            modelBuilder.Entity<TxRollupDispatchTicketsOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<TxRollupDispatchTicketsOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<TxRollupDispatchTicketsOperation>()
                .HasIndex(x => x.SenderId);

            modelBuilder.Entity<TxRollupDispatchTicketsOperation>()
                .HasIndex(x => x.RollupId);
            #endregion

            #region relations
            modelBuilder.Entity<TxRollupDispatchTicketsOperation>()
                .HasOne(x => x.Block)
                .WithMany(x => x.TxRollupDispatchTicketsOps)
                .HasForeignKey(x => x.Level)
                .HasPrincipalKey(x => x.Level);
            #endregion
        }
    }
}
