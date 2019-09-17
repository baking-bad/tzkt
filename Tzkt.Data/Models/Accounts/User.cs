using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class User : Account
    {
        public string PublicKey { get; set; }
        public long Counter { get; set; }

        #region indirect relations
        public List<Contract> ManagedContracts { get; set; }

        public ActivationOperation Activation { get; set; }
        public List<OriginationOperation> ManagedOriginations { get; set; }
        #endregion
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
