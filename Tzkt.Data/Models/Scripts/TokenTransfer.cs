using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Tzkt.Data.Models
{
    public class TokenTransfer
    {
        public long Id { get; set; }
        public int Level { get; set; }
        public int ContractId { get; set; }
        public long TokenId { get; set; }
        public BigInteger Amount { get; set; }

        public int? FromId { get; set; }
        public int? ToId { get; set; }

        public long? OriginationId { get; set; }
        public long? TransactionId { get; set; }
        public long? MigrationId { get; set; }
        public int? IndexedAt { get; set; }
    }

    public static class TokenTransferModel
    {
        public static void BuildTokenTransferModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<TokenTransfer>()
                .HasKey(x => x.Id);
            #endregion

            #region props
            // TODO: switch to `numeric` type after migration to .NET 6
            var converter = new ValueConverter<BigInteger, string>(
                x => x.ToString(),
                x => BigInteger.Parse(x));

            modelBuilder.Entity<TokenTransfer>()
                .Property(x => x.Amount)
                .HasConversion(converter);
            #endregion

            #region indexes
            modelBuilder.Entity<TokenTransfer>()
                .HasIndex(x => x.Id)
                .IsUnique();

            modelBuilder.Entity<TokenTransfer>()
                .HasIndex(x => x.ContractId);

            modelBuilder.Entity<TokenTransfer>()
                .HasIndex(x => x.TokenId);

            modelBuilder.Entity<TokenTransfer>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<TokenTransfer>()
                .HasIndex(x => x.IndexedAt)
                .HasFilter($@"""{nameof(TokenTransfer.IndexedAt)}"" is not null");

            modelBuilder.Entity<TokenTransfer>()
                .HasIndex(x => x.OriginationId)
                .HasFilter($@"""{nameof(TokenTransfer.OriginationId)}"" is not null");

            modelBuilder.Entity<TokenTransfer>()
                .HasIndex(x => x.TransactionId)
                .HasFilter($@"""{nameof(TokenTransfer.TransactionId)}"" is not null");

            modelBuilder.Entity<TokenTransfer>()
                .HasIndex(x => x.MigrationId)
                .HasFilter($@"""{nameof(TokenTransfer.MigrationId)}"" is not null");

            modelBuilder.Entity<TokenTransfer>()
                .HasIndex(x => x.FromId)
                .HasFilter($@"""{nameof(TokenTransfer.FromId)}"" is not null");

            modelBuilder.Entity<TokenTransfer>()
                .HasIndex(x => x.ToId)
                .HasFilter($@"""{nameof(TokenTransfer.ToId)}"" is not null");
            #endregion
        }
    }
}
