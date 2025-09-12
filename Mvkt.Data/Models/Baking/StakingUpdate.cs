using System.Numerics;
using Microsoft.EntityFrameworkCore;

namespace Mvkt.Data.Models
{
    public class StakingUpdate
    {
        public int Id { get; set; }
        public int Level { get; set; }
        public int Cycle { get; set; }
        public int BakerId { get; set; }
        public int StakerId { get; set; }
        public StakingUpdateType Type { get; set; }
        public long Amount { get; set; }
        
        public BigInteger? Pseudotokens { get; set; }
        public long? RoundingError { get; set; }

        public long? AutostakingOpId { get; set; }
        public long? StakingOpId { get; set; }
        public long? DelegationOpId { get; set; }
        public long? DoubleBakingOpId { get; set; }
        public long? DoubleEndorsingOpId { get; set; }
        public long? DoublePreendorsingOpId { get; set; }
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
                .HasIndex(x => x.DoubleEndorsingOpId)
                .HasFilter($@"""{nameof(StakingUpdate.DoubleEndorsingOpId)}"" IS NOT NULL");

            modelBuilder.Entity<StakingUpdate>()
                .HasIndex(x => x.DoublePreendorsingOpId)
                .HasFilter($@"""{nameof(StakingUpdate.DoublePreendorsingOpId)}"" IS NOT NULL");
            #endregion
        }
    }
}
