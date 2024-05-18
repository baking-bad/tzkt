using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class UnstakeRequest
    {
        public int Id { get; set; }
        public int Cycle { get; set; }
        public int BakerId { get; set; }
        public int? StakerId { get; set; }

        public long RequestedAmount { get; set; }
        public long RestakedAmount { get; set; }
        public long FinalizedAmount { get; set; }
        public long SlashedAmount { get; set; }

        public long? RoundingError { get; set; }

        public int UpdatesCount { get; set; }
        public int FirstLevel { get; set; }
        public int LastLevel { get; set; }
    }

    public static class UnstakeRequestModel
    {
        public static void BuildUnstakeRequestModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<UnstakeRequest>()
                .HasKey(x => x.Id);
            #endregion

            #region indexes
            modelBuilder.Entity<UnstakeRequest>()
                .HasIndex(x => new { x.BakerId, x.Cycle });

            modelBuilder.Entity<UnstakeRequest>()
                .HasIndex(x => new { x.StakerId, x.Cycle });
            #endregion
        }
    }
}
