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

            #region props
            modelBuilder.Entity<SetDepositsLimitOperation>()
                .Property(x => x.OpHash)
                .IsFixedLength(true)
                .HasMaxLength(51)
                .IsRequired();
            #endregion

            #region indexes
            modelBuilder.Entity<SetDepositsLimitOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<SetDepositsLimitOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<SetDepositsLimitOperation>()
                .HasIndex(x => new { x.SenderId, x.Id });
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
