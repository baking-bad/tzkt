using System.Numerics;
using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class Token
    {
        public required long Id { get; set; }
        public required int ContractId { get; set; }
        public required BigInteger TokenId { get; set; }
        public TokenTags Tags { get; set; }

        public required int FirstMinterId { get; set; }
        public required int FirstLevel { get; set; }
        public required int LastLevel { get; set; }

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

            // TODO: resolve issues with npgsql numeric-string mapping
            //// shadow property
            //modelBuilder.Entity<Token>()
            //    .Property<string>("Value")
            //    .HasColumnType("numeric");
            #endregion

            #region indexes
            modelBuilder.Entity<Token>()
                .HasIndex(x => new { x.ContractId, x.TokenId })
                .IsUnique();

            modelBuilder.Entity<Token>()
                .HasIndex(x => x.FirstMinterId);

            modelBuilder.Entity<Token>()
                .HasIndex(x => x.LastLevel);

            modelBuilder.Entity<Token>()
                .HasIndex(x => x.IndexedAt)
                .HasFilter($@"""{nameof(Token.IndexedAt)}"" IS NOT NULL");

            // shadow property
            modelBuilder.Entity<Token>()
                .HasIndex("Metadata")
                .HasMethod("gin")
                .HasOperators("jsonb_path_ops");
            #endregion
        }
    }
}
