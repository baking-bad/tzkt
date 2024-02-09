using Microsoft.EntityFrameworkCore;

namespace Mvkt.Data.Models
{
    public class SmartRollupCommitment
    {
        public int Id { get; set; }
        public int InitiatorId { get; set; }
        public int SmartRollupId { get; set; }
        public int? PredecessorId { get; set; }

        public int InboxLevel { get; set; }
        public string State { get; set; }
        public string Hash { get; set; }
        public long Ticks { get; set; }

        public int FirstLevel { get; set; }
        public int LastLevel { get; set; }

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
                .HasIndex(x => x.Hash);

            modelBuilder.Entity<SmartRollupCommitment>()
                .HasIndex(x => new { x.Hash, x.SmartRollupId });
            #endregion
        }
    }
}
