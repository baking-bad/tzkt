using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class User : Account
    {
        public bool Revealed { get; set; }
        public string PublicKey { get; set; }

        public long StakedBalance { get; set; }
        public long StakedPseudotokens { get; set; }
        public long UnstakedBalance { get; set; }
        public int? UnstakedBakerId { get; set; }
        
        public bool? Activated { get; set; }
        public int RegisterConstantsCount { get; set; }
        public int SetDepositsLimitsCount { get; set; }
        public int StakingOpsCount { get; set; }
    }

    public static class UserModel
    {
        public static void BuildUserModel(this ModelBuilder modelBuilder)
        {
            #region indexes
            modelBuilder.Entity<User>()
                .HasIndex(x => x.UnstakedBakerId)
                .HasFilter($@"""{nameof(User.UnstakedBakerId)}"" IS NOT NULL");
            #endregion
        }
    }
}
