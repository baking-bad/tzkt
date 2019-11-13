using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class VotingEpoch
    {
        public int Id { get; set; }
        public int Level { get; set; }
        public int Progress { get; set; }
    }

    public static class VotingEpochModel
    {
        public static void BuildVotingEpochModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<VotingEpoch>()
                .HasKey(x => x.Id);
            #endregion
        }
    }
}
