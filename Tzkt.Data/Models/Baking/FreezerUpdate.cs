using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class FreezerUpdate
    {
        public int Id { get; set; }
        public int Cycle { get; set; }
        public int BakerId { get; set; }
        public long Change { get; set; }
    }

    public static class FreezerUpdateModel
    {
        public static void BuildFreezerUpdateModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<FreezerUpdate>()
                .HasKey(x => x.Id);
            #endregion

            #region indexes
            modelBuilder.Entity<FreezerUpdate>()
                .HasIndex(x => x.Cycle);
            #endregion
        }
    }
}
