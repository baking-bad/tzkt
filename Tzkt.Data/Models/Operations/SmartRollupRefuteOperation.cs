using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    public class SmartRollupRefuteOperation : ManagerOperation
    {
        public int? SmartRollupId { get; set; }
    }

    public static class SmartRollupRefuteOperationModel
    {
        public static void BuildSmartRollupRefuteOperationModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<SmartRollupRefuteOperation>()
                .HasKey(x => x.Id);
            #endregion
            
            #region props
            modelBuilder.Entity<SmartRollupRefuteOperation>()
                .Property(x => x.OpHash)
                .IsFixedLength(true)
                .HasMaxLength(51)
                .IsRequired();
            #endregion

            #region indexes
            modelBuilder.Entity<SmartRollupRefuteOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<SmartRollupRefuteOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<SmartRollupRefuteOperation>()
                .HasIndex(x => x.SenderId);

            modelBuilder.Entity<SmartRollupRefuteOperation>()
                .HasIndex(x => x.SmartRollupId);
            #endregion

            #region relations
            modelBuilder.Entity<SmartRollupRefuteOperation>()
                .HasOne(x => x.Block)
                .WithMany(x => x.SmartRollupRefuteOps)
                .HasForeignKey(x => x.Level)
                .HasPrincipalKey(x => x.Level);
            #endregion
        }
    }
}
