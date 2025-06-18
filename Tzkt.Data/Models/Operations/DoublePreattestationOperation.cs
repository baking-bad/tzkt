using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    public class DoublePreattestationOperation : BaseOperation
    {
        public int AccusedLevel { get; set; }
        public int SlashedLevel { get; set; }

        public required int AccuserId { get; set; }
        public required int OffenderId { get; set; }
        
        public long Reward { get; set; }
        public long LostStaked { get; set; }
        public long LostUnstaked { get; set; }
        public long LostExternalStaked { get; set; }
        public long LostExternalUnstaked { get; set; }

        public int? StakingUpdatesCount { get; set; }
    }

    public static class DoublePreattestationOperationModel
    {
        public static void BuildDoublePreattestationOperationModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<DoublePreattestationOperation>()
                .HasKey(x => x.Id);
            #endregion
            
            #region props
            modelBuilder.Entity<DoublePreattestationOperation>()
                .Property(x => x.OpHash)
                .IsFixedLength(true)
                .HasMaxLength(51)
                .IsRequired();
            #endregion

            #region indexes
            modelBuilder.Entity<DoublePreattestationOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<DoublePreattestationOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<DoublePreattestationOperation>()
                .HasIndex(x => x.AccuserId);

            modelBuilder.Entity<DoublePreattestationOperation>()
                .HasIndex(x => x.OffenderId);
            #endregion
        }
    }
}
