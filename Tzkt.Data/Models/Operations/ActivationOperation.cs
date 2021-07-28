using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    public class ActivationOperation : BaseOperation
    {
        public int AccountId { get; set; }
        public long Balance { get; set; }

        #region relations
        [ForeignKey(nameof(AccountId))]
        public User Account { get; set; }
        #endregion
    }

    public static class ActivationOperationModel
    {
        public static void BuildActivationOperationModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<ActivationOperation>()
                .HasKey(x => x.Id);
            #endregion

            #region props
            modelBuilder.Entity<ActivationOperation>()
                .Property(x => x.OpHash)
                .IsFixedLength(true)
                .HasMaxLength(51)
                .IsRequired();
            #endregion

            #region indexes
            modelBuilder.Entity<ActivationOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<ActivationOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<ActivationOperation>()
                .HasIndex(x => x.AccountId)
                .IsUnique();
            #endregion

            #region relations
            modelBuilder.Entity<ActivationOperation>()
                .HasOne(x => x.Block)
                .WithMany(x => x.Activations)
                .HasForeignKey(x => x.Level)
                .HasPrincipalKey(x => x.Level);
            #endregion
        }
    }
}
