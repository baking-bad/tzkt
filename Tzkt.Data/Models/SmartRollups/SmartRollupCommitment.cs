using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class SmartRollupCommitment
    {
        public required int Id { get; set; }
        public required int InitiatorId { get; set; }
        public required int SmartRollupId { get; set; }
        public int? PredecessorId { get; set; }

        public required int InboxLevel { get; set; }
        public required string State { get; set; }
        public required string Hash { get; set; }
        public required long Ticks { get; set; }

        public required int FirstLevel { get; set; }
        public required int LastLevel { get; set; }

        public int Stakers { get; set; }
        public int ActiveStakers { get; set; }
        public int Successors { get; set; }
        public SmartRollupCommitmentStatus Status { get; set; }
    }

    public enum SmartRollupCommitmentStatus
    {
        Orphan,
        Refuted,
        Pending,
        Cemented,
        Executed
    }

    public static class SmartRollupCommitmentModel
    {
        public static void BuildSmartRollupCommitmentModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<SmartRollupCommitment>()
                .HasKey(x => x.Id);
            #endregion

            #region indexes
            modelBuilder.Entity<SmartRollupCommitment>()
                .HasIndex(x => x.PredecessorId);

            modelBuilder.Entity<SmartRollupCommitment>()
                .HasIndex(x => x.SmartRollupId);

            modelBuilder.Entity<SmartRollupCommitment>()
                .HasIndex(x => x.LastLevel);

            modelBuilder.Entity<SmartRollupCommitment>()
                .HasIndex(x => x.InboxLevel);

            modelBuilder.Entity<SmartRollupCommitment>()
                .HasIndex(x => new { x.Hash, x.SmartRollupId });
            #endregion
        }
    }
}
