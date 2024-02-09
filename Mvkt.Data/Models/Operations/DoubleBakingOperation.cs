using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Mvkt.Data.Models.Base;

namespace Mvkt.Data.Models
{
    public class DoubleBakingOperation : BaseOperation
    {
        public int AccusedLevel { get; set; }
        public int SlashedLevel { get; set; }

        public int AccuserId { get; set; }
        public int OffenderId { get; set; }

        public long Reward { get; set; }
        public long LostStaked { get; set; }
        public long LostUnstaked { get; set; }
        public long LostExternalStaked { get; set; }
        public long LostExternalUnstaked { get; set; }

        public int? StakingUpdatesCount { get; set; }

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
