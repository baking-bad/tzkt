using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class RevelationPenaltyOperation
    {
        public int Id { get; set; }
        public int Level { get; set; }
        public DateTime Timestamp { get; set; }

        public int BakerId { get; set; }
        public int MissedLevel { get; set; }

        public long LostReward { get; set; }
        public long LostFees { get; set; }

        #region relations
        [ForeignKey(nameof(Level))]
        public Block Block { get; set; }

        [ForeignKey(nameof(BakerId))]
        public Delegate Baker { get; set; }
        #endregion
    }

    public static class RevelationPenaltyOperationModel
    {
        public static void BuildRevelationPenaltyOperationModel(this ModelBuilder modelBuilder)
        {
            #region indexes
            modelBuilder.Entity<RevelationPenaltyOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<RevelationPenaltyOperation>()
                .HasIndex(x => x.BakerId);
            #endregion

            #region keys
            modelBuilder.Entity<RevelationPenaltyOperation>()
                .HasKey(x => x.Id);
            #endregion

            #region relations
            modelBuilder.Entity<RevelationPenaltyOperation>()
                .HasOne(x => x.Block)
                .WithMany(x => x.RevelationPenalties)
                .HasForeignKey(x => x.Level)
                .HasPrincipalKey(x => x.Level);
            #endregion
        }
    }
}
