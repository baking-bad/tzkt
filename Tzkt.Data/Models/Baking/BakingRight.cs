using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class BakingRight
    {
        #region settings
        public const int MaxRound = 7;
        #endregion

        public int Id { get; set; }
        public int Cycle { get; set; }
        public int Level { get; set; }
        public int BakerId { get; set; }
        public BakingRightType Type { get; set; }
        public BakingRightStatus Status { get; set; }
        public int? Round { get; set; }
        public int? Slots { get; set; }
    }

    public enum BakingRightType : byte
    {
        Baking = 0,
        Endorsing = 1
    }

    public enum BakingRightStatus : byte
    {
        Future = 0,
        Realized = 1,
        Missed = 2
    }

    public static class BakingRightModel
    {
        public static void BuildBakingRightModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<BakingRight>()
                .HasKey(x => x.Id);
            #endregion

            #region indexes
            modelBuilder.Entity<BakingRight>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<BakingRight>()
                .HasIndex(x => x.Cycle);

            modelBuilder.Entity<BakingRight>()
                .HasIndex(x => new { x.Cycle, x.BakerId });
            #endregion
        }
    }
}
