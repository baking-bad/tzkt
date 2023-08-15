using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Tzkt.Data.Models
{
    public class TicketTransfer
    {
        public long Id { get; set; }
        public int Level { get; set; }
        public int TicketerId { get; set; }
        public long TicketId { get; set; }
        public BigInteger Amount { get; set; }

        public int? FromId { get; set; }
        public int? ToId { get; set; }

        public long? TransferTicketId { get; set; }
        public long? TransactionId { get; set; }
        public long? SmartRollupExecuteId { get; set; }
        
        //TODO Do we need to add MigrationId here?

        public long? MigrationId { get; set; }
    }

    public static class TicketTransferModel
    {
        public static void BuildTicketTransferModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<TicketTransfer>()
                .HasKey(x => x.Id);
            #endregion

            #region props
            // TODO: switch to `numeric` type after migration to .NET 6
            var converter = new ValueConverter<BigInteger, string>(
                x => x.ToString(),
                x => BigInteger.Parse(x));

            modelBuilder.Entity<TicketTransfer>()
                .Property(x => x.Amount)
                .HasConversion(converter);
            #endregion

            #region indexes
            modelBuilder.Entity<TicketTransfer>()
                .HasIndex(x => x.Id)
                .IsUnique();

            modelBuilder.Entity<TicketTransfer>()
                .HasIndex(x => x.TicketerId);

            modelBuilder.Entity<TicketTransfer>()
                .HasIndex(x => x.TicketId);

            modelBuilder.Entity<TicketTransfer>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<TicketTransfer>()
                .HasIndex(x => x.SmartRollupExecuteId)
                .HasFilter($@"""{nameof(TicketTransfer.SmartRollupExecuteId)}"" is not null");

            modelBuilder.Entity<TicketTransfer>()
                .HasIndex(x => x.TransferTicketId)
                .HasFilter($@"""{nameof(TicketTransfer.TransferTicketId)}"" is not null");

            modelBuilder.Entity<TicketTransfer>()
                .HasIndex(x => x.TransactionId)
                .HasFilter($@"""{nameof(TicketTransfer.TransactionId)}"" is not null");

            modelBuilder.Entity<TicketTransfer>()
                .HasIndex(x => x.MigrationId)
                .HasFilter($@"""{nameof(TicketTransfer.MigrationId)}"" is not null");

            modelBuilder.Entity<TicketTransfer>()
                .HasIndex(x => x.FromId)
                .HasFilter($@"""{nameof(TicketTransfer.FromId)}"" is not null");

            modelBuilder.Entity<TicketTransfer>()
                .HasIndex(x => x.ToId)
                .HasFilter($@"""{nameof(TicketTransfer.ToId)}"" is not null");
            #endregion
        }
    }
}
