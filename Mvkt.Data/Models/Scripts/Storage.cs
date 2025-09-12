using Microsoft.EntityFrameworkCore;

namespace Mvkt.Data.Models
{
    public class Storage
    {
        public int Id { get; set; }
        public int Level { get; set; }
        public int ContractId { get; set; }
        public long? OriginationId { get; set; }
        public long? TransactionId { get; set; }
        public long? MigrationId { get; set; }
        public bool Current { get; set; }

        public byte[] RawValue { get; set; }
        public string JsonValue { get; set; }
    }

    public static class StorageModel
    {
        public static void BuildStorageModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<Storage>()
                .HasKey(x => x.Id);
            #endregion

            #region props
            modelBuilder.Entity<Storage>()
                .Property(x => x.JsonValue)
                .HasColumnType("jsonb");
            #endregion

            #region indexes
            modelBuilder.Entity<Storage>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<Storage>()
                .HasIndex(x => new { x.ContractId, x.Id });

            modelBuilder.Entity<Storage>()
                .HasIndex(x => x.ContractId, $"IX_{nameof(MvktContext.Storages)}_{nameof(Storage.ContractId)}_Partial")
                .HasFilter($@"""{nameof(Storage.Current)}"" = true");
            #endregion
        }
    }
}
