using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class BigMapKey
    {
        public int Id { get; set; }
        public int BigMapPtr { get; set; }
        public int FirstLevel { get; set; }
        public int LastLevel { get; set; }
        public int Updates { get; set; }
        public bool Active { get; set; }

        public string KeyHash { get; set; }
        public byte[] RawKey { get; set; }
        public string JsonKey { get; set; }

        public byte[] RawValue { get; set; }
        public string JsonValue { get; set; }
    }

    public static class BigMapKeyModel
    {
        public static void BuildBigMapKeyModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<BigMapKey>()
                .HasKey(x => x.Id);
            #endregion

            #region props
            modelBuilder.Entity<BigMapKey>()
                .Property(x => x.KeyHash)
                .HasMaxLength(54);

            modelBuilder.Entity<BigMapKey>()
                .Property(x => x.JsonKey)
                .HasColumnType("jsonb");

            modelBuilder.Entity<BigMapKey>()
                .Property(x => x.JsonValue)
                .HasColumnType("jsonb");
            #endregion

            #region indexes
            modelBuilder.Entity<BigMapKey>()
                .HasIndex(x => x.Id)
                .IsUnique();

            modelBuilder.Entity<BigMapKey>()
                .HasIndex(x => x.BigMapPtr);

            modelBuilder.Entity<BigMapKey>()
                .HasIndex(x => x.LastLevel);

            modelBuilder.Entity<BigMapKey>()
                .HasIndex(x => new { x.BigMapPtr, x.Active })
                .HasFilter($@"""{nameof(BigMapKey.Active)}"" = true");

            modelBuilder.Entity<BigMapKey>()
                .HasIndex(x => new { x.BigMapPtr, x.KeyHash });

            modelBuilder.Entity<BigMapKey>()
                .HasIndex(x => x.JsonKey)
                .HasMethod("gin")
                .HasOperators("jsonb_path_ops");

            modelBuilder.Entity<BigMapKey>()
                .HasIndex(x => x.JsonValue)
                .HasMethod("gin")
                .HasOperators("jsonb_path_ops");
            #endregion
        }
    }
}
