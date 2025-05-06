using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    public class RevelationPenaltyOperation : IOperation
    {
        public required long Id { get; set; }
        public required int Level { get; set; }
        public required DateTime Timestamp { get; set; }
        public required int BakerId { get; set; }

        public int MissedLevel { get; set; }
        public long Loss { get; set; }
    }

    public static class RevelationPenaltyOperationModel
    {
        public static void BuildRevelationPenaltyOperationModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<RevelationPenaltyOperation>()
                .HasKey(x => x.Id);
            #endregion

            #region indexes
            modelBuilder.Entity<RevelationPenaltyOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<RevelationPenaltyOperation>()
                .HasIndex(x => x.BakerId);
            #endregion
        }
    }
}
