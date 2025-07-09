using System.Numerics;
using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class StakingUpdate
    {
        public required int Id { get; set; }
        public required int Level { get; set; }
        public required int Cycle { get; set; }
        public required int BakerId { get; set; }
        public required int StakerId { get; set; }
        public required StakingUpdateType Type { get; set; }

        public long Amount { get; set; }
        
        public BigInteger? Pseudotokens { get; set; }
        public long? RoundingError { get; set; }

        public long? AutostakingOpId { get; set; }
        public long? StakingOpId { get; set; }
        public long? DelegationOpId { get; set; }
        public long? DoubleBakingOpId { get; set; }
        public long? DoubleConsensusOpId { get; set; }
    }

    public enum StakingUpdateType
    {
        Stake,
        Unstake,
        Restake,
        Finalize,
        SlashStaked,
        SlashUnstaked,
    }

    public static class StakingUpdateModel
    {
        public static void BuildStakingUpdateModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<StakingUpdate>()
                .HasKey(x => x.Id);
            #endregion

            #region indexes
            modelBuilder.Entity<StakingUpdate>()
                .HasIndex(x => new { x.Level, x.Id });

            modelBuilder.Entity<StakingUpdate>()
                .HasIndex(x => new { x.BakerId, x.Cycle, x.Id });

            modelBuilder.Entity<StakingUpdate>()
                .HasIndex(x => new { x.StakerId, x.Cycle, x.Id });

            modelBuilder.Entity<StakingUpdate>()
                .HasIndex(x => x.AutostakingOpId)
                .HasFilter($@"""{nameof(StakingUpdate.AutostakingOpId)}"" IS NOT NULL");

            modelBuilder.Entity<StakingUpdate>()
                .HasIndex(x => x.StakingOpId)
                .HasFilter($@"""{nameof(StakingUpdate.StakingOpId)}"" IS NOT NULL");

            modelBuilder.Entity<StakingUpdate>()
                .HasIndex(x => x.DelegationOpId)
                .HasFilter($@"""{nameof(StakingUpdate.DelegationOpId)}"" IS NOT NULL");

            modelBuilder.Entity<StakingUpdate>()
                .HasIndex(x => x.DoubleBakingOpId)
                .HasFilter($@"""{nameof(StakingUpdate.DoubleBakingOpId)}"" IS NOT NULL");

            modelBuilder.Entity<StakingUpdate>()
                .HasIndex(x => x.DoubleConsensusOpId)
                .HasFilter($@"""{nameof(StakingUpdate.DoubleConsensusOpId)}"" IS NOT NULL");
            #endregion
        }
    }
}
