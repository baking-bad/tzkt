using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class User : Account
    {
        public bool? Activated { get; set; }
        public string PublicKey { get; set; }
        public bool Revealed { get; set; }
        public int RegisterConstantsCount { get; set; }
        public int SetDepositsLimitsCount { get; set; }
    }

    public static class UserModel
    {
        public static void BuildUserModel(this ModelBuilder modelBuilder)
        {
            #region props
            modelBuilder.Entity<User>()
                .Property(x => x.PublicKey)
                .HasMaxLength(55);
            #endregion
        }
    }
}
