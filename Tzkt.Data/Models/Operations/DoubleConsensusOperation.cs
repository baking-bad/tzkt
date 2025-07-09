using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    public class DoubleConsensusOperation : BaseOperation
    {
        public DoubleConsensusKind Kind { get; set; }

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

    public enum DoubleConsensusKind
    {
        DoubleAttestation,
        DoublePreattestation
    }

    public static class DoubleConsensusOperationModel
    {
        public static void BuildDoubleConsensusOperationModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<DoubleConsensusOperation>()
                .HasKey(x => x.Id);
            #endregion
            
            #region props
            modelBuilder.Entity<DoubleConsensusOperation>()
                .Property(x => x.OpHash)
                .IsFixedLength(true)
                .HasMaxLength(51)
                .IsRequired();
            #endregion

            #region indexes
            modelBuilder.Entity<DoubleConsensusOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<DoubleConsensusOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<DoubleConsensusOperation>()
                .HasIndex(x => x.AccuserId);

            modelBuilder.Entity<DoubleConsensusOperation>()
                .HasIndex(x => x.OffenderId);
            #endregion
        }
    }
}
