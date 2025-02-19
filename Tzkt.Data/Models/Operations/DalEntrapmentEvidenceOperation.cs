using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    public class DalEntrapmentEvidenceOperation : BaseOperation
    {
        public int AccuserId { get; set; }
        public int OffenderId { get; set; }

        public int TrapLevel { get; set; }
        public int TrapSlotIndex { get; set; }
    }

    public static class DalEntrapmentEvidenceOperationModel
    {
        public static void BuildDalEntrapmentEvidenceOperationModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<DalEntrapmentEvidenceOperation>()
                .HasKey(x => x.Id);
            #endregion
            
            #region props
            modelBuilder.Entity<DalEntrapmentEvidenceOperation>()
                .Property(x => x.OpHash)
                .IsFixedLength(true)
                .HasMaxLength(51)
                .IsRequired();
            #endregion

            #region indexes
            modelBuilder.Entity<DalEntrapmentEvidenceOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<DalEntrapmentEvidenceOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<DalEntrapmentEvidenceOperation>()
                .HasIndex(x => x.AccuserId);

            modelBuilder.Entity<DalEntrapmentEvidenceOperation>()
                .HasIndex(x => x.OffenderId);
            #endregion

            #region relations
            modelBuilder.Entity<DalEntrapmentEvidenceOperation>()
                .HasOne(x => x.Block)
                .WithMany(x => x.DalEntrapmentEvidenceOps)
                .HasForeignKey(x => x.Level)
                .HasPrincipalKey(x => x.Level);
            #endregion
        }
    }
}
