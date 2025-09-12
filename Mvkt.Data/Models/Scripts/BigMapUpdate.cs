using Microsoft.EntityFrameworkCore;

namespace Mvkt.Data.Models
{
    public class BigMapUpdate
    {
        public int Id { get; set; }
        public int BigMapPtr { get; set; }
        public BigMapAction Action { get; set; }

        public int Level { get; set; }
        public long? OriginationId { get; set; }
        public long? TransactionId { get; set; }
        public long? MigrationId { get; set; }

        public int? BigMapKeyId { get; set; }
        public byte[] RawValue { get; set; }
        public string JsonValue { get; set; }
    }

    public enum BigMapAction
    {
        Allocate,
        AddKey,
        UpdateKey,
        RemoveKey,
        Remove
    }

    public static class BigMapUpdateModel
    {
        public static void BuildBigMapUpdateModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<BigMapUpdate>()
                .HasKey(x => x.Id);
            #endregion

            #region props
            modelBuilder.Entity<BigMapUpdate>()
                .Property(x => x.JsonValue)
                .HasColumnType("jsonb");
            #endregion

            #region indexes
            modelBuilder.Entity<BigMapUpdate>()
                .HasIndex(x => new { x.BigMapPtr, x.Id });

            modelBuilder.Entity<BigMapUpdate>()
                .HasIndex(x => new { x.BigMapKeyId, x.Id })
                .HasFilter($@"""{nameof(BigMapUpdate.BigMapKeyId)}"" IS NOT NULL");

            modelBuilder.Entity<BigMapUpdate>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<BigMapUpdate>()
                .HasIndex(x => x.OriginationId)
                .HasFilter($@"""{nameof(BigMapUpdate.OriginationId)}"" IS NOT NULL");

            modelBuilder.Entity<BigMapUpdate>()
                .HasIndex(x => x.TransactionId)
                .HasFilter($@"""{nameof(BigMapUpdate.TransactionId)}"" IS NOT NULL");

            modelBuilder.Entity<BigMapUpdate>()
                .HasIndex(x => x.MigrationId)
                .HasFilter($@"""{nameof(BigMapUpdate.MigrationId)}"" IS NOT NULL");
            #endregion
        }
    }
}
