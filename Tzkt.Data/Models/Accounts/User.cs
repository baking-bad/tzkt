using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class User : Account
    {
        public string PublicKey { get; set; }
        public bool? Activation { get; set; }
        public bool? AirDrop { get; set; }
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
