using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    public class OriginationOperation : InternalOperation
    {
        public int? ContractId { get; set; }
        public int? DelegateId { get; set; }

        public long Balance { get; set; }

        #region relations
        [ForeignKey(nameof(ContractId))]
        public Contract Contract { get; set; }

        [ForeignKey(nameof(DelegateId))]
        public Delegate Delegate { get; set; }
        #endregion

        #region indirect relations
        public WeirdDelegation WeirdDelegation { get; set; }
        #endregion
    }

    public static class OriginationOperationModel
    {
        public static void BuildOriginationOperationModel(this ModelBuilder modelBuilder)
        {
            #region indexes
            modelBuilder.Entity<OriginationOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<OriginationOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<OriginationOperation>()
                .HasIndex(x => x.SenderId);

            modelBuilder.Entity<OriginationOperation>()
                .HasIndex(x => x.ContractId);

            modelBuilder.Entity<OriginationOperation>()
                .HasIndex(x => x.DelegateId);
            #endregion
            
            #region keys
            modelBuilder.Entity<OriginationOperation>()
                .HasKey(x => x.Id);
            #endregion
            
            #region props
            modelBuilder.Entity<OriginationOperation>()
                .Property(x => x.OpHash)
                .IsFixedLength(true)
                .HasMaxLength(51)
                .IsRequired();
            #endregion
            
            #region relations
            modelBuilder.Entity<OriginationOperation>()
                .HasOne(x => x.Block)
                .WithMany(x => x.Originations)
                .HasForeignKey(x => x.Level)
                .HasPrincipalKey(x => x.Level);

            modelBuilder.Entity<OriginationOperation>()
                .HasOne(x => x.Sender)
                .WithMany(x => x.SentOriginations)
                .HasForeignKey(x => x.SenderId);

            modelBuilder.Entity<OriginationOperation>()
                .HasOne(x => x.Parent)
                .WithMany(x => x.InternalOriginations)
                .HasForeignKey(x => x.ParentId);

            modelBuilder.Entity<OriginationOperation>()
                .HasOne(x => x.Contract)
                .WithOne(x => x.Origination)
                .HasForeignKey<OriginationOperation>(x => x.ContractId);

            modelBuilder.Entity<OriginationOperation>()
                .HasOne(x => x.Delegate)
                .WithMany(x => x.DelegatedOriginations)
                .HasForeignKey(x => x.DelegateId);
            #endregion
        }
    }
}
