using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class VotingSnapshot
    {
        public required int Id { get; set; }
        public required int Level { get; set; }
        public required int Period { get; set; }
        public required int BakerId { get; set; }
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
