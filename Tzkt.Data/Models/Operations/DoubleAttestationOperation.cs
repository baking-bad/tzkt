using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    public class DoubleAttestationOperation : BaseOperation
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

    public static class DoubleAttestationOperationModel
    {
        public static void BuildDoubleAttestationOperationModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<DoubleAttestationOperation>()
                .HasKey(x => x.Id);
            #endregion
            
            #region props
            modelBuilder.Entity<DoubleAttestationOperation>()
                .Property(x => x.OpHash)
                .IsFixedLength(true)
                .HasMaxLength(51)
                .IsRequired();
            #endregion

            #region indexes
            modelBuilder.Entity<DoubleAttestationOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<DoubleAttestationOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<DoubleAttestationOperation>()
                .HasIndex(x => x.AccuserId);

            modelBuilder.Entity<DoubleAttestationOperation>()
                .HasIndex(x => x.OffenderId);
            #endregion
        }
    }
}
