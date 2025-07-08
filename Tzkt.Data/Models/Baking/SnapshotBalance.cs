using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;
using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class SnapshotBalance
    {
        public required int Level { get; set; }
        public required int BakerId { get; set; }
        public required int AccountId { get; set; }
        
        public int? DelegatorsCount { get; set; }
        public long OwnDelegatedBalance { get; set; }
        public long? ExternalDelegatedBalance { get; set; }

        public long? OwnStakedBalance { get; set; }
        public long? ExternalStakedBalance { get; set; }
        public int? StakersCount { get; set; }

        public BigInteger? Pseudotokens { get; set; }

        #region helpers
        [NotMapped]
        public long StakingBalance => OwnDelegatedBalance + (ExternalDelegatedBalance ?? 0) + (OwnStakedBalance ?? 0) + (ExternalStakedBalance ?? 0);
        #endregion
    }

    public static class SnapshotBalanceModel
    {
        public static void BuildSnapshotBalanceModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<SnapshotBalance>()
                .HasKey(x => new { x.Level, x.BakerId, x.AccountId });
            #endregion

            #region indexes
            modelBuilder.Entity<SnapshotBalance>()
                .HasIndex(x => x.Level, $"IX_{nameof(TzktContext.SnapshotBalances)}_{nameof(SnapshotBalance.Level)}_Partial")
                .HasFilter(@"""BakerId"" = ""AccountId""");
            #endregion
        }
    }
}
