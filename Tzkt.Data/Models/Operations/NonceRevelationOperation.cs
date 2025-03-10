using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    public class NonceRevelationOperation : BaseOperation
    {
        public int BakerId { get; set; }
        public int SenderId { get; set; }
        public int RevealedLevel { get; set; }
        public int RevealedCycle { get; set; }
        public long RewardDelegated { get; set; }
        public long RewardStakedOwn { get; set; }
        public long RewardStakedEdge { get; set; }
        public long RewardStakedShared { get; set; }
        public byte[] Nonce { get; set; }
    }

    public static class NonceRevelationOperationModel
    {
        public static void BuildNonceRevelationOperationModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<NonceRevelationOperation>()
                .HasKey(x => x.Id);
            #endregion
            
            #region props
            modelBuilder.Entity<NonceRevelationOperation>()
                .Property(x => x.OpHash)
                .IsFixedLength(true)
                .HasMaxLength(51)
                .IsRequired();

            modelBuilder.Entity<NonceRevelationOperation>()
                .Property(x => x.Nonce)
                .IsFixedLength(true)
                .HasMaxLength(32)
                .IsRequired();
            #endregion

            #region indexes
            modelBuilder.Entity<NonceRevelationOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<NonceRevelationOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<NonceRevelationOperation>()
                .HasIndex(x => x.BakerId);

            modelBuilder.Entity<NonceRevelationOperation>()
                .HasIndex(x => x.SenderId);

            modelBuilder.Entity<NonceRevelationOperation>()
                .HasIndex(x => x.RevealedCycle);
            #endregion
        }
    }
}
