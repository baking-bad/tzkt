using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
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
            // TODO: switch to `numeric` type after migration to .NET 6
            var converter = new ValueConverter<BigInteger?, string>(
                x => x == null ? null : x.ToString(),
                x => x == null ? null : BigInteger.Parse(x));

            modelBuilder.Entity<SetDepositsLimitOperation>()
                .Property(x => x.Limit)
                .HasConversion(converter);
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
