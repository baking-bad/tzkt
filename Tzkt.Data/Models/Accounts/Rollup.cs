using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class Rollup : Account
    {
        [Column("CreatorId")]
        public int CreatorId { get; set; }
    }

    public static class RollupModel
    { 
        public static void BuildRollupModel(this ModelBuilder modelBuilder)
        {
            #region indexes
            modelBuilder.Entity<Rollup>()
                .HasIndex(x => x.CreatorId);
            #endregion
        }
    }
}
