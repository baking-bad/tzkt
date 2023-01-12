using System;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Tzkt.Data.Models
{
    public class Token
    {
        public long Id { get; set; }
        public int ContractId { get; set; }
        public BigInteger TokenId { get; set; }
        public TokenTags Tags { get; set; }

        public int FirstMinterId { get; set; }
        public int FirstLevel { get; set; }
        public int LastLevel { get; set; }

        public int TransfersCount { get; set; }
        public int BalancesCount { get; set; }
        public int HoldersCount { get; set; }

        public BigInteger TotalMinted { get; set; }
        public BigInteger TotalBurned { get; set; }
        public BigInteger TotalSupply { get; set; }

        public int? OwnerId { get; set; }
        public int? IndexedAt { get; set; }
    }

    [Flags]
    public enum TokenTags
    {
        None    = 0b_0000,
        Fa12    = 0b_0001,
        Fa2     = 0b_0010,
        Nft     = 0b_0110
    }

    public static class TokenModel
    {
        public static void BuildTokenModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<Token>()
                .HasKey(x => x.Id);
            #endregion

            #region props
            // shadow property
            modelBuilder.Entity<Token>()
                .Property<string>("Metadata")
                .HasColumnType("jsonb");

            // TODO: switch to `numeric` type after migration to .NET 6
            var converter = new ValueConverter<BigInteger, string>(
                x => x.ToString(),
                x => BigInteger.Parse(x));

            modelBuilder.Entity<Token>()
                .Property(x => x.TokenId)
                .HasConversion(converter);

            modelBuilder.Entity<Token>()
                .Property(x => x.TotalMinted)
                .HasConversion(converter);

            modelBuilder.Entity<Token>()
                .Property(x => x.TotalBurned)
                .HasConversion(converter);

            modelBuilder.Entity<Token>()
                .Property(x => x.TotalSupply)
                .HasConversion(converter);
            #endregion

            #region indexes
            modelBuilder.Entity<Token>()
                .HasIndex(x => x.Id)
                .IsUnique();

            modelBuilder.Entity<Token>()
                .HasIndex(x => x.ContractId);

            modelBuilder.Entity<Token>()
                .HasIndex(x => new { x.ContractId, x.TokenId })
                .IsUnique();

            modelBuilder.Entity<Token>()
                .HasIndex(x => x.FirstMinterId);

            modelBuilder.Entity<Token>()
                .HasIndex(x => x.LastLevel);

            modelBuilder.Entity<Token>()
                .HasIndex(x => x.IndexedAt)
                .HasFilter($@"""{nameof(Token.IndexedAt)}"" is not null");

            // shadow property
            modelBuilder.Entity<Token>()
                .HasIndex("Metadata")
                .HasMethod("gin")
                .HasOperators("jsonb_path_ops");
            #endregion
        }
    }
}
