using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    public class TransactionOperation : ContractOperation
    {
        public int? SenderCodeHash { get; set; }
        public int? TargetId { get; set; }
        public int? TargetCodeHash { get; set; }
        public int? ResetDeactivation { get; set; }

        public long Amount { get; set; }

        public string Entrypoint { get; set; }
        public byte[] RawParameters { get; set; }
        public string JsonParameters { get; set; }

        public short? InternalOperations { get; set; }
        public short? InternalDelegations { get; set; }
        public short? InternalOriginations { get; set; }
        public short? InternalTransactions { get; set; }

        public int? EventsCount { get; set; }
        public int? TicketTransfers { get; set; }
    }

    public static class TransactionOperationModel
    {
        public static void BuildTransactionOperationModel(this ModelBuilder modelBuilder)
        {
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

            #region indexes
            modelBuilder.Entity<TransactionOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<TransactionOperation>()
                .HasIndex(x => x.TargetId, $"IX_{nameof(TzktContext.TransactionOps)}_{nameof(TransactionOperation.TargetId)}_Partial")
                .HasFilter($"""
                    "{nameof(TransactionOperation.Entrypoint)}" = 'transfer'
                    AND "{nameof(TransactionOperation.TokenTransfers)}" IS NULL
                    AND "{nameof(TransactionOperation.Status)}" = {(int)OperationStatus.Applied}
                    """);

            modelBuilder.Entity<TransactionOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<TransactionOperation>()
                .HasIndex(x => x.SenderId);

            modelBuilder.Entity<TransactionOperation>()
                .HasIndex(x => x.SenderCodeHash)
                .HasFilter($@"""{nameof(TransactionOperation.SenderCodeHash)}"" IS NOT NULL");

            modelBuilder.Entity<TransactionOperation>()
                .HasIndex(x => x.InitiatorId);

            modelBuilder.Entity<TransactionOperation>()
                .HasIndex(x => x.TargetId);

            modelBuilder.Entity<TransactionOperation>()
                .HasIndex(x => x.TargetCodeHash)
                .HasFilter($@"""{nameof(TransactionOperation.TargetCodeHash)}"" IS NOT NULL");

            modelBuilder.Entity<TransactionOperation>()
                .HasIndex(x => x.JsonParameters)
                .HasMethod("gin")
                .HasOperators("jsonb_path_ops");
            #endregion
        }
    }
}
