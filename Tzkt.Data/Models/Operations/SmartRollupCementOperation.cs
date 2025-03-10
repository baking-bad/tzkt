using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    public class SmartRollupCementOperation : ManagerOperation
    {
        public int? SmartRollupId { get; set; }
        public int? CommitmentId { get; set; }
    }

    public static class SmartRollupCementOperationModel
    {
        public static void BuildSmartRollupCementOperationModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<SmartRollupCementOperation>()
                .HasKey(x => x.Id);
            #endregion
            
            #region props
            modelBuilder.Entity<SmartRollupCementOperation>()
                .Property(x => x.OpHash)
                .IsFixedLength(true)
                .HasMaxLength(51)
                .IsRequired();
            #endregion

            #region indexes
            modelBuilder.Entity<SmartRollupCementOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<SmartRollupCementOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<SmartRollupCementOperation>()
                .HasIndex(x => new { x.SmartRollupId, x.Id });

            modelBuilder.Entity<SmartRollupCementOperation>()
                .HasIndex(x => x.SenderId);

            modelBuilder.Entity<SmartRollupCementOperation>()
                .HasIndex(x => x.CommitmentId);
            #endregion
        }
    }
}
