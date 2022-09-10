using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Tzkt.Data.Models
{
    public class TokenBalance
    {
        public long Id { get; set; }
        public int ContractId { get; set; }
        public long TokenId { get; set; }
        public int AccountId { get; set; }
        public int FirstLevel { get; set; }
        public int LastLevel { get; set; }
        public int TransfersCount { get; set; }
        public BigInteger Balance { get; set; }
        public int? IndexedAt { get; set; }
    }

    public static class TokenBalanceModel
    {
        public static void BuildTokenBalanceModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<TokenBalance>()
                .HasKey(x => x.Id);
            #endregion

            #region props
            // TODO: switch to `numeric` type after migration to .NET 6
            var converter = new ValueConverter<BigInteger, string>(
                x => x.ToString(),
                x => BigInteger.Parse(x));

            modelBuilder.Entity<TokenBalance>()
                .Property(x => x.Balance)
                .HasConversion(converter);
            #endregion

            #region indexes
            modelBuilder.Entity<TokenBalance>()
                .HasIndex(x => x.Id)
                .IsUnique();

            modelBuilder.Entity<TokenBalance>()
                .HasIndex(x => x.ContractId);

            modelBuilder.Entity<TokenBalance>()
                .HasIndex(x => x.ContractId)
                .HasFilter($@"""{nameof(TokenBalance.Balance)}"" != '0'");

            modelBuilder.Entity<TokenBalance>()
                .HasIndex(x => x.TokenId);

            modelBuilder.Entity<TokenBalance>()
                .HasIndex(x => x.TokenId)
                .HasFilter($@"""{nameof(TokenBalance.Balance)}"" != '0'");

            modelBuilder.Entity<TokenBalance>()
                .HasIndex(x => x.AccountId);

            modelBuilder.Entity<TokenBalance>()
                .HasIndex(x => x.AccountId)
                .HasFilter($@"""{nameof(TokenBalance.Balance)}"" != '0'");

            modelBuilder.Entity<TokenBalance>()
                .HasIndex(x => new { x.AccountId, x.ContractId });

            modelBuilder.Entity<TokenBalance>()
                .HasIndex(x => new { x.AccountId, x.TokenId })
                .IsUnique();

            modelBuilder.Entity<TokenBalance>()
                .HasIndex(x => x.LastLevel);

            modelBuilder.Entity<TokenBalance>()
                .HasIndex(x => x.IndexedAt)
                .HasFilter($@"""{nameof(TokenBalance.IndexedAt)}"" is not null");
            #endregion
        }
    }
}
