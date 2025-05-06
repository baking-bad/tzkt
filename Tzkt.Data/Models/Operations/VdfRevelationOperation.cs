using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    public class VdfRevelationOperation : BaseOperation
    {
        public required int BakerId { get; set; }
        public int Cycle { get; set; }
        public long RewardDelegated { get; set; }
        public long RewardStakedOwn { get; set; }
        public long RewardStakedEdge { get; set; }
        public long RewardStakedShared { get; set; }
        public required byte[] Solution { get; set; }
        public required byte[] Proof { get; set; }
    }

    public static class VdfRevelationOperationModel
    {
        public static void BuildVdfRevelationOperationModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<VdfRevelationOperation>()
                .HasKey(x => x.Id);
            #endregion
            
            #region props
            modelBuilder.Entity<VdfRevelationOperation>()
                .Property(x => x.OpHash)
                .IsFixedLength(true)
                .HasMaxLength(51)
                .IsRequired();
            #endregion

            #region indexes
            modelBuilder.Entity<VdfRevelationOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<VdfRevelationOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<VdfRevelationOperation>()
                .HasIndex(x => x.BakerId);

            modelBuilder.Entity<VdfRevelationOperation>()
                .HasIndex(x => new { x.Cycle, x.Id });
            #endregion
        }
    }
}
