using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    public class SmartRollupAddMessagesOperation : ManagerOperation
    {
        public int MessagesCount { get; set; }
    }

    public static class SmartRollupAddMessagesOperationModel
    {
        public static void BuildSmartRollupAddMessagesOperationModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<SmartRollupAddMessagesOperation>()
                .HasKey(x => x.Id);
            #endregion
            
            #region props
            modelBuilder.Entity<SmartRollupAddMessagesOperation>()
                .Property(x => x.OpHash)
                .IsFixedLength(true)
                .HasMaxLength(51)
                .IsRequired();
            #endregion

            #region indexes
            modelBuilder.Entity<SmartRollupAddMessagesOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<SmartRollupAddMessagesOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<SmartRollupAddMessagesOperation>()
                .HasIndex(x => x.SenderId);
            #endregion

            #region relations
            modelBuilder.Entity<SmartRollupAddMessagesOperation>()
                .HasOne(x => x.Block)
                .WithMany(x => x.SmartRollupAddMessagesOps)
                .HasForeignKey(x => x.Level)
                .HasPrincipalKey(x => x.Level);
            #endregion
        }
    }
}
