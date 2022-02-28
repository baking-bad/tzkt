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
        public long Reward { get; set; }
        public byte[] Nonce { get; set; }

        #region relations
        [ForeignKey(nameof(BakerId))]
        public Delegate Baker { get; set; }

        [ForeignKey(nameof(SenderId))]
        public Delegate Sender { get; set; }
        #endregion

        #region indirect relations
        public Block RevealedBlock { get; set; }
        #endregion
    }

    public static class NonceRevelationOperationModel
    {
        public static void BuildNonceRevelationOperationModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<NonceRevelationOperation>()
                .HasKey(x => x.Id);

            modelBuilder.Entity<NonceRevelationOperation>()
                .HasAlternateKey(x => x.RevealedLevel);
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

            #region relations
            modelBuilder.Entity<NonceRevelationOperation>()
                .HasOne(x => x.Block)
                .WithMany(x => x.Revelations)
                .HasForeignKey(x => x.Level)
                .HasPrincipalKey(x => x.Level);
            #endregion
        }
    }
}
