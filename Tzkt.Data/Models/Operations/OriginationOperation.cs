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
        }
    }
}
