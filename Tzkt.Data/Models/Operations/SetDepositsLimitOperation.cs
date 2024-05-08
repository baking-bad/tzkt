using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    public class SetDepositsLimitOperation : ManagerOperation
    {
        public BigInteger? Limit { get; set; }
    }

    public static class SetDepositsLimitOperationModel
    {
        public static void BuildSetDepositsLimitOperationModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<SetDepositsLimitOperation>()
                .HasKey(x => x.Id);
            #endregion

            #region indexes
            modelBuilder.Entity<SetDepositsLimitOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<SetDepositsLimitOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<SetDepositsLimitOperation>()
                .HasIndex(x => x.SenderId);
            #endregion

            #region relations
            modelBuilder.Entity<SetDepositsLimitOperation>()
                .HasOne(x => x.Block)
                .WithMany(x => x.SetDepositsLimits)
                .HasForeignKey(x => x.Level)
                .HasPrincipalKey(x => x.Level);
            #endregion
        }
    }
}
