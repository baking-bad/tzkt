using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    //TODO: store a mask in the Slots field
    public class EndorsementOperation : BaseOperation
    {
        public int DelegateId { get; set; }
        public int Slots { get; set; }
        public long Reward { get; set; }

        public int? ResetDeactivation { get; set; }

        #region relations
        [ForeignKey(nameof(DelegateId))]
        public Delegate Delegate { get; set; }
        #endregion
    }

    public static class EndorsementOperationModel
    {
        public static void BuildEndorsementOperationModel(this ModelBuilder modelBuilder)
        {
            #region indexes
            modelBuilder.Entity<EndorsementOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<EndorsementOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<EndorsementOperation>()
                .HasIndex(x => x.DelegateId);
            #endregion
            
            #region keys
            modelBuilder.Entity<EndorsementOperation>()
                .HasKey(x => x.Id);
            #endregion
            
            #region props
            modelBuilder.Entity<EndorsementOperation>()
                .Property(x => x.OpHash)
                .IsFixedLength(true)
                .HasMaxLength(51)
                .IsRequired();
            #endregion
            
            #region relations
            modelBuilder.Entity<EndorsementOperation>()
                .HasOne(x => x.Block)
                .WithMany(x => x.Endorsements)
                .HasForeignKey(x => x.Level)
                .HasPrincipalKey(x => x.Level);
            #endregion
        }
    }
}
