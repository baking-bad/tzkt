using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class RefutationGame
    {
        public int Id { get; set; }
        public int SmartRollupId { get; set; }
        public int InitiatorId { get; set; }
        public int OpponentId { get; set; }
        public int InitiatorCommitmentId { get; set; }
        public int OpponentCommitmentId { get; set; }
        public long LastMoveId { get; set; }
        
        public int FirstLevel { get; set; }
        public int LastLevel { get; set; }

        public long? InitiatorReward { get; set; }
        public long? InitiatorLoss { get; set; }
        public long? OpponentReward { get; set; }
        public long? OpponentLoss { get; set; }
    }

    public static class RefutationGameModel
    {
        public static void BuildRefutationGameModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<RefutationGame>()
                .HasKey(x => x.Id);
            #endregion

            #region indexes
            modelBuilder.Entity<RefutationGame>()
                .HasIndex(x => new { x.SmartRollupId, x.Id });

            modelBuilder.Entity<RefutationGame>()
                .HasIndex(x => x.InitiatorId);

            modelBuilder.Entity<RefutationGame>()
                .HasIndex(x => x.InitiatorCommitmentId);

            modelBuilder.Entity<RefutationGame>()
                .HasIndex(x => x.OpponentId);

            modelBuilder.Entity<RefutationGame>()
                .HasIndex(x => x.OpponentCommitmentId);

            modelBuilder.Entity<RefutationGame>()
                .HasIndex(x => x.FirstLevel);

            modelBuilder.Entity<RefutationGame>()
                .HasIndex(x => x.LastLevel);
            #endregion
        }
    }
}
