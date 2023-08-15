using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Tzkt.Data.Models
{
    public class TicketBalance
    {
        public long Id { get; set; }
        
        //TODO Do we need ticketer here?
        public int TicketerId { get; set; }
        public long TicketId { get; set; }
        public int AccountId { get; set; }
        public int FirstLevel { get; set; }
        public int LastLevel { get; set; }
        public int TransfersCount { get; set; }
        public BigInteger Balance { get; set; }
    }

    public static class TicketBalanceModel
    {
        public static void BuildTicketBalanceModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<TicketBalance>()
                .HasKey(x => x.Id);
            #endregion

            #region props
            // TODO: switch to `numeric` type after migration to .NET 6
            var converter = new ValueConverter<BigInteger, string>(
                x => x.ToString(),
                x => BigInteger.Parse(x));

            modelBuilder.Entity<TicketBalance>()
                .Property(x => x.Balance)
                .HasConversion(converter);
            #endregion

            #region indexes
            modelBuilder.Entity<TicketBalance>()
                .HasIndex(x => x.Id)
                .IsUnique();

            modelBuilder.Entity<TicketBalance>()
                .HasIndex(x => x.TicketerId);

            modelBuilder.Entity<TicketBalance>()
                .HasIndex(x => x.TicketerId)
                .HasFilter($@"""{nameof(TicketBalance.Balance)}"" != '0'");

            modelBuilder.Entity<TicketBalance>()
                .HasIndex(x => x.TicketId);

            modelBuilder.Entity<TicketBalance>()
                .HasIndex(x => x.TicketId)
                .HasFilter($@"""{nameof(TicketBalance.Balance)}"" != '0'");

            modelBuilder.Entity<TicketBalance>()
                .HasIndex(x => x.AccountId);

            modelBuilder.Entity<TicketBalance>()
                .HasIndex(x => x.AccountId)
                .HasFilter($@"""{nameof(TicketBalance.Balance)}"" != '0'");

            modelBuilder.Entity<TicketBalance>()
                .HasIndex(x => new { x.AccountId, x.TicketerId });

            modelBuilder.Entity<TicketBalance>()
                .HasIndex(x => new { x.AccountId, x.TicketId })
                .IsUnique();

            modelBuilder.Entity<TicketBalance>()
                .HasIndex(x => x.LastLevel);
            #endregion
        }
    }
}
