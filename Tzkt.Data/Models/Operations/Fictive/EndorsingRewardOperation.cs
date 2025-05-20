using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    public class EndorsingRewardOperation : IOperation
    {
        public required long Id { get; set; }
        public required int Level { get; set; }
        public required DateTime Timestamp { get; set; }
        public required int BakerId { get; set; }

        public long Expected { get; set; }
        public long RewardDelegated { get; set; }
        public long RewardStakedOwn { get; set; }
        public long RewardStakedEdge { get; set; }
        public long RewardStakedShared { get; set; }
    }

    public static class EndorsingRewardOperationModel
    {
        public static void BuildEndorsingRewardOperationModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<EndorsingRewardOperation>()
                .HasKey(x => x.Id);
            #endregion

            #region indexes
            modelBuilder.Entity<EndorsingRewardOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<EndorsingRewardOperation>()
                .HasIndex(x => x.BakerId);
            #endregion
        }
    }
}
