using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class VotingSnapshot
    {
        public int Id { get; set; }
        public int Level { get; set; }
        public int PeriodId { get; set; }
        public int DelegateId { get; set; }

        public int Rolls { get; set; }

        #region relations
        [ForeignKey(nameof(PeriodId))]
        public VotingPeriod Period { get; set; }
        #endregion
    }

    public static class VotingSnapshotModel
    {
        public static void BuildVotingSnapshotModel(this ModelBuilder modelBuilder)
        {
            #region indexes
            modelBuilder.Entity<VotingSnapshot>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<VotingSnapshot>()
                .HasIndex(x => new { x.PeriodId, x.DelegateId });
            #endregion

            #region keys
            modelBuilder.Entity<VotingEpoch>()
                .HasKey(x => x.Id);
            #endregion
        }
    }
}
