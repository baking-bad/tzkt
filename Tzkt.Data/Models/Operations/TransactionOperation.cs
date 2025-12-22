using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
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

        public string? Entrypoint { get; set; }
        public byte[]? RawParameters { get; set; }
        public string? JsonParameters { get; set; }

        public short? InternalOperations { get; set; }
        public short? InternalDelegations { get; set; }
        public short? InternalOriginations { get; set; }
        public short? InternalTransactions { get; set; }

        public int? EventsCount { get; set; }
        public int? TicketTransfers { get; set; }
        public int? AddressRegistryIndex { get; set; }

        #region binary writer
        public static void Write(NpgsqlConnection conn, IEnumerable<TransactionOperation> ops)
        {
            using var writer = conn.BeginBinaryImport($"""
                COPY "{nameof(TzktContext.TransactionOps)}" (
                    "{nameof(Id)}",
                    "{nameof(SenderCodeHash)}",
                    "{nameof(TargetId)}",
                    "{nameof(TargetCodeHash)}",
                    "{nameof(ResetDeactivation)}",
                    "{nameof(Amount)}",
                    "{nameof(Entrypoint)}",
                    "{nameof(RawParameters)}",
                    "{nameof(JsonParameters)}",
                    "{nameof(InternalOperations)}",
                    "{nameof(InternalDelegations)}",
                    "{nameof(InternalOriginations)}",
                    "{nameof(InternalTransactions)}",
                    "{nameof(EventsCount)}",
                    "{nameof(TicketTransfers)}",
                    "{nameof(AddressRegistryIndex)}",
                    "{nameof(Level)}",
                    "{nameof(Timestamp)}",
                    "{nameof(OpHash)}",
                    "{nameof(SenderId)}",
                    "{nameof(Counter)}",
                    "{nameof(BakerFee)}",
                    "{nameof(StorageFee)}",
                    "{nameof(AllocationFee)}",
                    "{nameof(GasLimit)}",
                    "{nameof(GasUsed)}",
                    "{nameof(StorageLimit)}",
                    "{nameof(StorageUsed)}",
                    "{nameof(Status)}",
                    "{nameof(Errors)}",
                    "{nameof(InitiatorId)}",
                    "{nameof(Nonce)}",
                    "{nameof(StorageId)}",
                    "{nameof(BigMapUpdates)}",
                    "{nameof(TokenTransfers)}",
                    "{nameof(SubIds)}"
                )
                FROM STDIN (FORMAT BINARY)
                """);

            foreach (var op in ops)
            {
                writer.StartRow();

                writer.Write(op.Id, NpgsqlDbType.Bigint);
                writer.WriteNullable(op.SenderCodeHash, NpgsqlDbType.Integer);
                writer.WriteNullable(op.TargetId, NpgsqlDbType.Integer);
                writer.WriteNullable(op.TargetCodeHash, NpgsqlDbType.Integer);
                writer.WriteNullable(op.ResetDeactivation, NpgsqlDbType.Integer);
                writer.Write(op.Amount, NpgsqlDbType.Bigint);
                writer.WriteNullable(op.Entrypoint, NpgsqlDbType.Text);
                writer.WriteNullable(op.RawParameters, NpgsqlDbType.Bytea);
                writer.WriteNullable(op.JsonParameters, NpgsqlDbType.Jsonb);
                writer.WriteNullable(op.InternalOperations, NpgsqlDbType.Smallint);
                writer.WriteNullable(op.InternalDelegations, NpgsqlDbType.Smallint);
                writer.WriteNullable(op.InternalOriginations, NpgsqlDbType.Smallint);
                writer.WriteNullable(op.InternalTransactions, NpgsqlDbType.Smallint);
                writer.WriteNullable(op.EventsCount, NpgsqlDbType.Integer);
                writer.WriteNullable(op.TicketTransfers, NpgsqlDbType.Integer);
                writer.WriteNullable(op.AddressRegistryIndex, NpgsqlDbType.Integer);
                writer.Write(op.Level, NpgsqlDbType.Integer);
                writer.Write(op.Timestamp, NpgsqlDbType.TimestampTz);
                writer.Write(op.OpHash, NpgsqlDbType.Char);
                writer.Write(op.SenderId, NpgsqlDbType.Integer);
                writer.Write(op.Counter, NpgsqlDbType.Integer);
                writer.Write(op.BakerFee, NpgsqlDbType.Bigint);
                writer.WriteNullable(op.StorageFee, NpgsqlDbType.Bigint);
                writer.WriteNullable(op.AllocationFee, NpgsqlDbType.Bigint);
                writer.Write(op.GasLimit, NpgsqlDbType.Integer);
                writer.Write(op.GasUsed, NpgsqlDbType.Integer);
                writer.Write(op.StorageLimit, NpgsqlDbType.Integer);
                writer.Write(op.StorageUsed, NpgsqlDbType.Integer);
                writer.Write((int)op.Status, NpgsqlDbType.Smallint);
                writer.WriteNullable(op.Errors, NpgsqlDbType.Text);
                writer.WriteNullable(op.InitiatorId, NpgsqlDbType.Integer);
                writer.WriteNullable(op.Nonce, NpgsqlDbType.Integer);
                writer.WriteNullable(op.StorageId, NpgsqlDbType.Integer);
                writer.WriteNullable(op.BigMapUpdates, NpgsqlDbType.Integer);
                writer.WriteNullable(op.TokenTransfers, NpgsqlDbType.Integer);
                writer.WriteNullable(op.SubIds, NpgsqlDbType.Integer);
            }

            writer.Complete();
        }
        #endregion
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
                .HasFilter($@"""{nameof(TransactionOperation.Entrypoint)}"" = 'transfer' AND ""{nameof(TransactionOperation.TokenTransfers)}"" IS NULL AND ""{nameof(TransactionOperation.Status)}"" = {(int)OperationStatus.Applied}");

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
