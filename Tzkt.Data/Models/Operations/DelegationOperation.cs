using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    public class DelegationOperation : InternalOperation
    {
        public int? DelegateId { get; set; }
        public int? ResetDeactivation { get; set; }

        #region relations
        [ForeignKey(nameof(DelegateId))]
        public Delegate Delegate { get; set; }
        #endregion
    }

    public static class DelegationOperationModel
    {
        public static void BuildDelegationOperationModel(this ModelBuilder modelBuilder)
        {
            #region indexes
            modelBuilder.Entity<DelegationOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<DelegationOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<DelegationOperation>()
                .HasIndex(x => x.SenderId);

            modelBuilder.Entity<DelegationOperation>()
                .HasIndex(x => x.DelegateId);
            #endregion

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
