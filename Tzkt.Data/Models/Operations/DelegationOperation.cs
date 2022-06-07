using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    //TODO: add delegation amount
    public class DelegationOperation : InternalOperation
    {
        public int? SenderCodeHash { get; set; }
        public int? DelegateId { get; set; }
        public int? PrevDelegateId { get; set; }
        public int? ResetDeactivation { get; set; }

        public long Amount { get; set; }

        #region relations
        [ForeignKey(nameof(DelegateId))]
        public Delegate Delegate { get; set; }

        [ForeignKey(nameof(PrevDelegateId))]
        public Delegate PrevDelegate { get; set; }
        #endregion
    }

    public static class DelegationOperationModel
    {
        public static void BuildDelegationOperationModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<DelegationOperation>()
                .HasKey(x => x.Id);
            #endregion

            #region props
            modelBuilder.Entity<DelegationOperation>()
                .Property(x => x.OpHash)
                .IsFixedLength(true)
                .HasMaxLength(51)
                .IsRequired();
            #endregion

            #region indexes
            modelBuilder.Entity<DelegationOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<DelegationOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<DelegationOperation>()
                .HasIndex(x => x.SenderId);

            modelBuilder.Entity<DelegationOperation>()
                .HasIndex(x => x.SenderCodeHash)
                .HasFilter($@"""{nameof(DelegationOperation.SenderCodeHash)}"" IS NOT NULL");

            modelBuilder.Entity<DelegationOperation>()
                .HasIndex(x => x.InitiatorId);

            modelBuilder.Entity<DelegationOperation>()
                .HasIndex(x => x.DelegateId);

            modelBuilder.Entity<DelegationOperation>()
                .HasIndex(x => x.PrevDelegateId);
            #endregion

            #region relations
            modelBuilder.Entity<DelegationOperation>()
                .HasOne(x => x.Block)
                .WithMany(x => x.Delegations)
                .HasForeignKey(x => x.Level)
                .HasPrincipalKey(x => x.Level);
            #endregion
        }
    }
}
