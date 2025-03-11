using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class SnapshotBalance
    {
        public required int Id { get; set; }
        public required int Level { get; set; }
        public required int AccountId { get; set; }
        public required int BakerId { get; set; }
        
        public long OwnDelegatedBalance { get; set; }
        public long ExternalDelegatedBalance { get; set; }
        public int DelegatorsCount { get; set; }

        public long OwnStakedBalance { get; set; }
        public long ExternalStakedBalance { get; set; }
        public int StakersCount { get; set; }

        #region helpers
        [NotMapped]
        public long StakingBalance => OwnDelegatedBalance + ExternalDelegatedBalance + OwnStakedBalance + ExternalStakedBalance;
        #endregion
    }

    public static class SnapshotBalanceModel
    {
        public static void BuildSnapshotBalanceModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<SnapshotBalance>()
                .HasKey(x => x.Id);
            #endregion

            #region indexes
            modelBuilder.Entity<SnapshotBalance>()
                .HasIndex(x => x.Level, $"IX_{nameof(TzktContext.SnapshotBalances)}_{nameof(SnapshotBalance.Level)}_Partial")
                .HasFilter(@"""AccountId"" = ""BakerId""");

            modelBuilder.Entity<SnapshotBalance>()
                .HasIndex(x => new { x.Level, x.AccountId, x.BakerId });
            #endregion
        }
    }
}
