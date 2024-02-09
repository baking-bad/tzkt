using System.Numerics;
using Microsoft.EntityFrameworkCore;

namespace Mvkt.Data.Models
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

            #region indexes
            modelBuilder.Entity<TokenTransfer>()
                .HasIndex(x => x.ContractId);

            modelBuilder.Entity<TokenTransfer>()
                .HasIndex(x => x.TokenId);

            modelBuilder.Entity<TokenTransfer>()
                .HasIndex(x => new { x.Level, x.Id });

            modelBuilder.Entity<TokenTransfer>()
                .HasIndex(x => x.IndexedAt)
                .HasFilter($@"""{nameof(TokenTransfer.IndexedAt)}"" IS NOT NULL");

            modelBuilder.Entity<TokenTransfer>()
                .HasIndex(x => x.OriginationId)
                .HasFilter($@"""{nameof(TokenTransfer.OriginationId)}"" IS NOT NULL");

            modelBuilder.Entity<TokenTransfer>()
                .HasIndex(x => x.TransactionId)
                .HasFilter($@"""{nameof(TokenTransfer.TransactionId)}"" IS NOT NULL");

            modelBuilder.Entity<TokenTransfer>()
                .HasIndex(x => x.MigrationId)
                .HasFilter($@"""{nameof(TokenTransfer.MigrationId)}"" IS NOT NULL");

            modelBuilder.Entity<TokenTransfer>()
                .HasIndex(x => x.FromId)
                .HasFilter($@"""{nameof(TokenTransfer.FromId)}"" IS NOT NULL");

            modelBuilder.Entity<TokenTransfer>()
                .HasIndex(x => x.ToId)
                .HasFilter($@"""{nameof(TokenTransfer.ToId)}"" IS NOT NULL");
            #endregion
        }
    }
}
