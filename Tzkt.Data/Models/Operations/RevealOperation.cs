using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    public class RevealOperation : ManagerOperation
    {
    }

    public static class RevealOperationModel
    {
        public static void BuildRevealOperationModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<RevealOperation>()
                .HasKey(x => x.Id);
            #endregion
            
            #region props
            modelBuilder.Entity<RevealOperation>()
                .Property(x => x.OpHash)
                .IsFixedLength(true)
                .HasMaxLength(51)
                .IsRequired();
            #endregion

            #region indexes
            modelBuilder.Entity<RevealOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<RevealOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<RevealOperation>()
                .HasIndex(x => x.SenderId);
            #endregion

            #region relations
            modelBuilder.Entity<RevealOperation>()
                .HasOne(x => x.Block)
                .WithMany(x => x.Reveals)
                .HasForeignKey(x => x.Level)
                .HasPrincipalKey(x => x.Level);
            #endregion
        }
    }
}
