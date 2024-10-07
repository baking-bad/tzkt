using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class DalRight
    {
        public int Id { get; set; }
        public int Cycle { get; set; }
        public int Level { get; set; }
        public int DelegateId { get; set; }
        public int Shards { get; set; }
    }

    public static class DalRightModel
    {
        public static void BuildDalRightModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<DalRight>()
                .HasKey(x => x.Id);
            #endregion

            #region indexes
            modelBuilder.Entity<DalRight>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<DalRight>()
                .HasIndex(x => new { x.Cycle, x.DelegateId });
            #endregion
        }
    }
}
