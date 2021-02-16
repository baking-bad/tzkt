using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class Storage
    {
        public int Id { get; set; }
        public int Level { get; set; }
        public int ContractId { get; set; }
        public int? OriginationId { get; set; }
        public int? TransactionId { get; set; }
        public int? MigrationId { get; set; }
        public bool Current { get; set; }

        public byte[] RawValue { get; set; }
        public string JsonValue { get; set; }
    }

    public static class StorageModel
    {
        public static void BuildStorageModel(this ModelBuilder modelBuilder)
        {
            #region indexes
            modelBuilder.Entity<Storage>()
                .HasIndex(x => x.Id)
                .IsUnique();

            modelBuilder.Entity<Storage>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<Storage>()
                .HasIndex(x => x.ContractId);
            
            modelBuilder.Entity<Storage>()
                .HasIndex(x => new { x.ContractId, x.Current })
                .HasFilter($@"""{nameof(Storage.Current)}"" = true");
            #endregion

            #region keys
            modelBuilder.Entity<Storage>()
                .HasKey(x => x.Id);
            #endregion

            #region props
            modelBuilder.Entity<Storage>()
                .Property(x => x.JsonValue)
                .HasColumnType("jsonb");
            #endregion
        }
    }
}
