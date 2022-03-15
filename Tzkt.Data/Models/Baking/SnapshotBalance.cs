using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class SnapshotBalance
    {
        public int Id { get; set; }
        public int Level { get; set; }
        public long Balance { get; set; }
        public int AccountId { get; set; }
        public int? DelegateId { get; set; }
        public int? DelegatorsCount { get; set; }
        public long? DelegatedBalance { get; set; }
        public long? StakingBalance { get; set; }
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
                .HasIndex(x => x.Level);

            modelBuilder.Entity<SnapshotBalance>()
                .HasIndex(x => x.Level)
                .HasFilter(@"""DelegateId"" IS NULL");
            #endregion
        }
    }
}
