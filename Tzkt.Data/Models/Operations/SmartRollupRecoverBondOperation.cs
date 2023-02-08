using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    public class SmartRollupRecoverBondOperation : ManagerOperation
    {
        public int? SmartRollupId { get; set; }
    }

    public static class SmartRollupRecoverBondOperationModel
    {
        public static void BuildSmartRollupRecoverBondOperationModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<SmartRollupRecoverBondOperation>()
                .HasKey(x => x.Id);
            #endregion
            
            #region props
            modelBuilder.Entity<SmartRollupRecoverBondOperation>()
                .Property(x => x.OpHash)
                .IsFixedLength(true)
                .HasMaxLength(51)
                .IsRequired();
            #endregion

            #region indexes
            modelBuilder.Entity<SmartRollupRecoverBondOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<SmartRollupRecoverBondOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<SmartRollupRecoverBondOperation>()
                .HasIndex(x => x.SenderId);

            modelBuilder.Entity<SmartRollupRecoverBondOperation>()
                .HasIndex(x => x.SmartRollupId);
            #endregion

            #region relations
            modelBuilder.Entity<SmartRollupRecoverBondOperation>()
                .HasOne(x => x.Block)
                .WithMany(x => x.SmartRollupRecoverBondOps)
                .HasForeignKey(x => x.Level)
                .HasPrincipalKey(x => x.Level);
            #endregion
        }
    }
}
