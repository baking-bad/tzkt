using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    public class PreattestationOperation : BaseOperation
    {
        public required int DelegateId { get; set; }
        public long Power { get; set; }
    }

    public static class PreattestationOperationModel
    {
        public static void BuildPreattestationOperationModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<PreattestationOperation>()
                .HasKey(x => x.Id);
            #endregion
            
            #region props
            modelBuilder.Entity<PreattestationOperation>()
                .Property(x => x.OpHash)
                .IsFixedLength(true)
                .HasMaxLength(51)
                .IsRequired();
            #endregion

            #region indexes
            modelBuilder.Entity<PreattestationOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<PreattestationOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<PreattestationOperation>()
                .HasIndex(x => x.DelegateId);
            #endregion
        }
    }
}
