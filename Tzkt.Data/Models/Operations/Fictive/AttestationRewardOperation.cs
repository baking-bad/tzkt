using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    public class AttestationRewardOperation : IOperation
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

    public static class AttestationRewardOperationModel
    {
        public static void BuildAttestationRewardOperationModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<AttestationRewardOperation>()
                .HasKey(x => x.Id);
            #endregion

            #region indexes
            modelBuilder.Entity<AttestationRewardOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<AttestationRewardOperation>()
                .HasIndex(x => x.BakerId);
            #endregion
        }
    }
}
