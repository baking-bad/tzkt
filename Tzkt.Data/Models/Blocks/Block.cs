using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class Block
    {
        public required long Id { get; set; }
        public required int Cycle { get; set; }
        public required int Level { get; set; }
        public required string Hash { get; set; }
        public required DateTime Timestamp { get; set; }
        public required int ProtoCode { get; set; }

        public int? SoftwareId { get; set; }

        public int PayloadRound { get; set; }
        public int BlockRound { get; set; }
        public int Validations { get; set; }
        public BlockEvents Events { get; set; }
        public Operations Operations { get; set; }

        public long Deposit { get; set; }
        public long RewardDelegated { get; set; }
        public long RewardStakedOwn { get; set; }
        public long RewardStakedEdge { get; set; }
        public long RewardStakedShared { get; set; }
        public long BonusDelegated { get; set; }
        public long BonusStakedOwn { get; set; }
        public long BonusStakedEdge { get; set; }
        public long BonusStakedShared { get; set; }
        public long Fees { get; set; }

        public int? ProposerId { get; set; }
        public int? ProducerId { get; set; }
        public long? RevelationId { get; set; }
        public int? ResetBakerDeactivation { get; set; }
        public int? ResetProposerDeactivation { get; set; }

        public bool? LBToggle { get; set; }
        public int LBToggleEma { get; set; }

        public bool? AIToggle { get; set; }
        public int AIToggleEma { get; set; }
    }

    public static class BlockModel
    {
        public static void BuildBlockModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<Block>()
                .HasKey(x => x.Id);
            #endregion

            #region props
            modelBuilder.Entity<Block>()
                .Property(x => x.Hash)
                .IsFixedLength(true)
                .HasMaxLength(51)
                .IsRequired();

            // shadow property
            modelBuilder.Entity<Block>()
                .Property<string>("Extras")
                .HasColumnType("jsonb");
            #endregion

            #region indexes
            modelBuilder.Entity<Block>()
                .HasIndex(x => x.Level)
                .IsUnique();

            modelBuilder.Entity<Block>()
                .HasIndex(x => x.Hash)
                .IsUnique();

            modelBuilder.Entity<Block>()
                .HasIndex(x => x.ProposerId);

            modelBuilder.Entity<Block>()
                .HasIndex(x => x.ProducerId);
            #endregion
        }
    }
}
