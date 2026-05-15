using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    public class IncreasePaidStorageOperation : ManagerOperation
    {
        public int ContractId { get; set; }
        public BigInteger Amount { get; set; }
    }

    public static class IncreasePaidStorageOperationModel
    {
        public static void BuildIncreasePaidStorageOperationModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<IncreasePaidStorageOperation>()
                .HasKey(x => x.Id);
            #endregion

            #region props
            modelBuilder.Entity<IncreasePaidStorageOperation>()
                .Property(x => x.OpHash)
                .IsFixedLength(true)
                .HasMaxLength(51)
                .IsRequired();
            #endregion

            #region indexes
            modelBuilder.Entity<IncreasePaidStorageOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<IncreasePaidStorageOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<IncreasePaidStorageOperation>()
                .HasIndex(x => x.SenderId);

            modelBuilder.Entity<IncreasePaidStorageOperation>()
                .HasIndex(x => x.ContractId);
            #endregion
        }
    }
}
