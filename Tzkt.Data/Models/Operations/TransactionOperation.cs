using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    public class TransactionOperation : InternalOperation
    {
        public int? TargetId { get; set; }
        public int? ResetDeactivation { get; set; }

        public long Amount { get; set; }

        public string Entrypoint { get; set; }
        public byte[] RawParameters { get; set; }
        public string JsonParameters { get; set; }

        public int? StorageId { get; set; }

        public short? InternalOperations { get; set; }
        public short? InternalDelegations { get; set; }
        public short? InternalOriginations { get; set; }
        public short? InternalTransactions { get; set; }

        #region relations
        [ForeignKey(nameof(TargetId))]
        public Account Target { get; set; }

        [ForeignKey(nameof(StorageId))]
        public Storage Storage { get; set; }
        #endregion
    }

    public static class TransactionOperationModel
    {
        public static void BuildTransactionOperationModel(this ModelBuilder modelBuilder)
        {
            #region indexes
            modelBuilder.Entity<TransactionOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<TransactionOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<TransactionOperation>()
                .HasIndex(x => x.SenderId);

            modelBuilder.Entity<TransactionOperation>()
                .HasIndex(x => x.InitiatorId);

            modelBuilder.Entity<TransactionOperation>()
                .HasIndex(x => x.TargetId);
            #endregion

            #region keys
            modelBuilder.Entity<TransactionOperation>()
                .HasKey(x => x.Id);
            #endregion
            
            #region props
            modelBuilder.Entity<TransactionOperation>()
                .Property(x => x.OpHash)
                .IsFixedLength(true)
                .HasMaxLength(51)
                .IsRequired();

            modelBuilder.Entity<TransactionOperation>()
                .Property(x => x.JsonParameters)
                .HasColumnType("jsonb");
            #endregion
            
            #region relations
            modelBuilder.Entity<TransactionOperation>()
                .HasOne(x => x.Block)
                .WithMany(x => x.Transactions)
                .HasForeignKey(x => x.Level)
                .HasPrincipalKey(x => x.Level);
            #endregion
        }
    }
}
