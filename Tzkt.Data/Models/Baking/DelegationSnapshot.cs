using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class DelegationSnapshot
    {
        public required int Level { get; set; }
        public required int BakerId { get; set; }
        public required int AccountId { get; set; }
        
        public long OwnDelegatedBalance { get; set; }
        public long? ExternalDelegatedBalance { get; set; }
        public int? DelegatorsCount { get; set; }

        public int? PrevMinTotalDelegatedLevel { get; set; }
        public long? PrevMinTotalDelegated {  get; set; }
    }

    public static class DelegationSnapshotModel
    {
        public static void BuildDelegationSnapshotModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<DelegationSnapshot>()
                .HasKey(x => new { x.Level, x.BakerId, x.AccountId });
            #endregion

            #region indexes
            modelBuilder.Entity<DelegationSnapshot>()
                .HasIndex(x => x.Level, $"IX_{nameof(TzktContext.DelegationSnapshots)}_{nameof(DelegationSnapshot.Level)}_Partial")
                .HasFilter(@"""BakerId"" = ""AccountId""");
            #endregion
        }
    }
}
