using System.Numerics;
using Microsoft.EntityFrameworkCore;

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

            #region indexes
            modelBuilder.Entity<TokenBalance>()
                .HasIndex(x => x.ContractId);

            modelBuilder.Entity<TokenBalance>()
                .HasIndex(x => x.ContractId, $"IX_{nameof(TzktContext.TokenBalances)}_{nameof(TokenBalance.ContractId)}_Partial")
                .HasFilter($@"""{nameof(TokenBalance.Balance)}"" != '0'");

            modelBuilder.Entity<TokenBalance>()
                .HasIndex(x => x.TokenId);

            modelBuilder.Entity<TokenBalance>()
                .HasIndex(x => x.TokenId, $"IX_{nameof(TzktContext.TokenBalances)}_{nameof(TokenBalance.TokenId)}_Partial")
                .HasFilter($@"""{nameof(TokenBalance.Balance)}"" != '0'");

            modelBuilder.Entity<TokenBalance>()
                .HasIndex(x => x.AccountId, $"IX_{nameof(TzktContext.TokenBalances)}_{nameof(TokenBalance.AccountId)}_Partial")
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
                .HasFilter($@"""{nameof(TokenBalance.IndexedAt)}"" IS NOT NULL");
            #endregion
        }
    }
}
