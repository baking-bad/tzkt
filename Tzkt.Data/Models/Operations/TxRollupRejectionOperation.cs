using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    public class TxRollupRejectionOperation : ManagerOperation
    {
        public int? RollupId { get; set; }
        public int CommitterId { get; set; }
        public long Reward { get; set; }
        public long Loss { get; set; }
    }

    public static class TxRollupRejectionOperationModel
    {
        public static void BuildTxRollupRejectionOperationModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<TxRollupRejectionOperation>()
                .HasKey(x => x.Id);
            #endregion
            
            #region props
            modelBuilder.Entity<TxRollupRejectionOperation>()
                .Property(x => x.OpHash)
                .IsFixedLength(true)
                .HasMaxLength(51)
                .IsRequired();
            #endregion

            #region indexes
            modelBuilder.Entity<TxRollupRejectionOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<TxRollupRejectionOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<TxRollupRejectionOperation>()
                .HasIndex(x => x.SenderId);

            modelBuilder.Entity<TxRollupRejectionOperation>()
                .HasIndex(x => x.RollupId);

            modelBuilder.Entity<TxRollupRejectionOperation>()
                .HasIndex(x => x.CommitterId);
            #endregion

            #region relations
            modelBuilder.Entity<TxRollupRejectionOperation>()
                .HasOne(x => x.Block)
                .WithMany(x => x.TxRollupRejectionOps)
                .HasForeignKey(x => x.Level)
                .HasPrincipalKey(x => x.Level);
            #endregion
        }
    }
}
