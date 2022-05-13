using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class VotingSnapshot
    {
        public int Id { get; set; }
        public int Level { get; set; }
        public int Period { get; set; }
        public int BakerId { get; set; }
        public long VotingPower { get; set; }

        public VoterStatus Status { get; set; }
    }

    public static class VotingSnapshotModel
    {
        public static void BuildVotingSnapshotModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<VotingSnapshot>()
                .HasKey(x => x.Id);
            #endregion

            #region indexes
            modelBuilder.Entity<VotingSnapshot>()
                .HasIndex(x => x.Period);

            modelBuilder.Entity<VotingSnapshot>()
                .HasIndex(x => new { x.Period, x.BakerId })
                .IsUnique();
            #endregion
        }
    }

    public enum VoterStatus
    {
        None,
        Upvoted,
        VotedYay,
        VotedNay,
        VotedPass
    }
}
