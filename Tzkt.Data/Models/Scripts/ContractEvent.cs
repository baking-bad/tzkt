using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class ContractEvent
    {
        public required int Id { get; set; }
        public required int Level { get; set; }
        public required int ContractId { get; set; }
        public required int ContractCodeHash { get; set; }
        public required long TransactionId { get; set; }

        public string? Tag { get; set; }
        public byte[]? Type { get; set; }
        public byte[]? RawPayload { get; set; }
        public string? JsonPayload { get; set; }
    }

    public static class ContractEventModel
    {
        public static void BuildContractEventModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<ContractEvent>()
                .HasKey(x => x.Id);
            #endregion

            #region props
            modelBuilder.Entity<ContractEvent>()
                .Property(x => x.JsonPayload)
                .HasColumnType("jsonb");
            #endregion

            #region indexes
            modelBuilder.Entity<ContractEvent>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<ContractEvent>()
                .HasIndex(x => x.TransactionId);

            modelBuilder.Entity<ContractEvent>()
                .HasIndex(x => x.Tag);

            modelBuilder.Entity<ContractEvent>()
                .HasIndex(x => new { x.ContractId, x.Tag });

            modelBuilder.Entity<ContractEvent>()
                .HasIndex(x => new { x.ContractCodeHash, x.Tag });

            modelBuilder.Entity<ContractEvent>()
                .HasIndex(x => x.JsonPayload)
                .HasMethod("gin")
                .HasOperators("jsonb_path_ops");
            #endregion
        }
    }
}
