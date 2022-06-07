using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    public class OriginationOperation : ContractOperation
    {
        public int? SenderCodeHash { get; set; }
        public int? ManagerId { get; set; }
        public int? DelegateId { get; set; }
        public int? ContractId { get; set; }
        public int? ContractCodeHash { get; set; }
        public int? ScriptId { get; set; }

        public long Balance { get; set; }

        #region relations
        [ForeignKey(nameof(ContractId))]
        public Contract Contract { get; set; }

        [ForeignKey(nameof(DelegateId))]
        public Delegate Delegate { get; set; }

        [ForeignKey(nameof(ManagerId))]
        public User Manager { get; set; }

        [ForeignKey(nameof(ScriptId))]
        public Script Script { get; set; }

        [ForeignKey(nameof(StorageId))]
        public Storage Storage { get; set; }
        #endregion
    }

    public static class OriginationOperationModel
    {
        public static void BuildOriginationOperationModel(this ModelBuilder modelBuilder)
        {
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

            #region indexes
            modelBuilder.Entity<OriginationOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<OriginationOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<OriginationOperation>()
                .HasIndex(x => x.SenderId);

            modelBuilder.Entity<OriginationOperation>()
                .HasIndex(x => x.SenderCodeHash)
                .HasFilter($@"""{nameof(OriginationOperation.SenderCodeHash)}"" IS NOT NULL");

            modelBuilder.Entity<OriginationOperation>()
                .HasIndex(x => x.InitiatorId);

            modelBuilder.Entity<OriginationOperation>()
                .HasIndex(x => x.ManagerId);

            modelBuilder.Entity<OriginationOperation>()
                .HasIndex(x => x.DelegateId);

            modelBuilder.Entity<OriginationOperation>()
                .HasIndex(x => x.ContractId);

            modelBuilder.Entity<OriginationOperation>()
                .HasIndex(x => x.ContractCodeHash)
                .HasFilter($@"""{nameof(OriginationOperation.ContractCodeHash)}"" IS NOT NULL");
            #endregion

            #region relations
            modelBuilder.Entity<OriginationOperation>()
                .HasOne(x => x.Block)
                .WithMany(x => x.Originations)
                .HasForeignKey(x => x.Level)
                .HasPrincipalKey(x => x.Level);
            #endregion
        }
    }
}
