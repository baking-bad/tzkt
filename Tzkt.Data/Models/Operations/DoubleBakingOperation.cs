using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    public class DoubleBakingOperation : BaseOperation
    {
        public int AccusedLevel { get; set; }

        public int AccuserId { get; set; }
        public long AccuserReward { get; set; }

        public int OffenderId { get; set; }
        public long OffenderLoss { get; set; }

        #region relations
        [ForeignKey(nameof(AccuserId))]
        public Delegate Accuser { get; set; }

        [ForeignKey(nameof(OffenderId))]
        public Delegate Offender { get; set; }
        #endregion
    }

    public static class DoubleBakingOperationModel
    {
        public static void BuildDoubleBakingOperationModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<DoubleBakingOperation>()
                .HasKey(x => x.Id);
            #endregion
            
            #region props
            modelBuilder.Entity<DoubleBakingOperation>()
                .Property(x => x.OpHash)
                .IsFixedLength(true)
                .HasMaxLength(51)
                .IsRequired();
            #endregion

            #region indexes
            modelBuilder.Entity<DoubleBakingOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<DoubleBakingOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<DoubleBakingOperation>()
                .HasIndex(x => x.AccuserId);

            modelBuilder.Entity<DoubleBakingOperation>()
                .HasIndex(x => x.OffenderId);
            #endregion

            #region relations
            modelBuilder.Entity<DoubleBakingOperation>()
                .HasOne(x => x.Block)
                .WithMany(x => x.DoubleBakings)
                .HasForeignKey(x => x.Level)
                .HasPrincipalKey(x => x.Level);
            #endregion
        }
    }
}
