using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Tzkt.Data.Models
{
    public class Software
    {
        public int Id { get; set; }
        public int BlocksCount { get; set; }
        public int FirstLevel { get; set; }
        public int LastLevel { get; set; }
        public string ShortHash { get; set; }

        public string Metadata { get; set; }
    }

    public static class SoftwareModel
    {
        public static void BuildSoftwareModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<Software>()
                .HasKey(x => x.Id);
            #endregion

            #region props
            modelBuilder.Entity<Software>()
                .Property(x => x.ShortHash)
                .IsFixedLength(true)
                .HasMaxLength(8)
                .IsRequired();

            modelBuilder.Entity<Software>()
                .Property(x => x.Metadata)
                .HasColumnType("jsonb");

            modelBuilder.Entity<Software>()
                .Property(x => x.Metadata)
                .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            #endregion
        }
    }
}
