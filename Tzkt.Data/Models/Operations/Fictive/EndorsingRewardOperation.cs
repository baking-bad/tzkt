using System;
using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class EndorsingRewardOperation
    {
        public long Id { get; set; }
        public int Level { get; set; }
        public DateTime Timestamp { get; set; }

        public int BakerId { get; set; }
        public long Expected { get; set; }
        public long Received { get; set; }
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
