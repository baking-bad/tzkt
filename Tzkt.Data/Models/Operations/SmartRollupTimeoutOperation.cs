using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    public class SmartRollupTimeoutOperation : ManagerOperation
    {
        public int? SmartRollupId { get; set; }
    }

    public static class SmartRollupTimeoutOperationModel
    {
        public static void BuildSmartRollupTimeoutOperationModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<SmartRollupTimeoutOperation>()
                .HasKey(x => x.Id);
            #endregion
            
            #region props
            modelBuilder.Entity<SmartRollupTimeoutOperation>()
                .Property(x => x.OpHash)
                .IsFixedLength(true)
                .HasMaxLength(51)
                .IsRequired();
            #endregion

            #region indexes
            modelBuilder.Entity<SmartRollupTimeoutOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<SmartRollupTimeoutOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<SmartRollupTimeoutOperation>()
                .HasIndex(x => x.SenderId);

            modelBuilder.Entity<SmartRollupTimeoutOperation>()
                .HasIndex(x => x.SmartRollupId);
            #endregion

            #region relations
            modelBuilder.Entity<SmartRollupTimeoutOperation>()
                .HasOne(x => x.Block)
                .WithMany(x => x.SmartRollupTimeoutOps)
                .HasForeignKey(x => x.Level)
                .HasPrincipalKey(x => x.Level);
            #endregion
        }
    }
}
