using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    public class AttestationOperation : BaseOperation
    {
        public required int DelegateId { get; set; }
        public long Power { get; set; }
        public long Reward { get; set; }
        public long Deposit { get; set; }

        public int? ResetDeactivation { get; set; }

        #region binary writer
        public static void Write(NpgsqlConnection conn, IEnumerable<AttestationOperation> ops)
        {
            using var writer = conn.BeginBinaryImport($"""
                COPY "{nameof(TzktContext.AttestationOps)}" (
                    "{nameof(Id)}",
                    "{nameof(DelegateId)}",
                    "{nameof(Power)}",
                    "{nameof(Reward)}",
                    "{nameof(Deposit)}",
                    "{nameof(ResetDeactivation)}",
                    "{nameof(Level)}",
                    "{nameof(Timestamp)}",
                    "{nameof(OpHash)}"
                )
                FROM STDIN (FORMAT BINARY)
                """);

            foreach (var op in ops)
            {
                writer.StartRow();

                writer.Write(op.Id, NpgsqlDbType.Bigint);
                writer.Write(op.DelegateId, NpgsqlDbType.Integer);
                writer.Write(op.Power, NpgsqlDbType.Bigint);
                writer.Write(op.Reward, NpgsqlDbType.Bigint);
                writer.Write(op.Deposit, NpgsqlDbType.Bigint);
                writer.WriteNullable(op.ResetDeactivation, NpgsqlDbType.Integer);
                writer.Write(op.Level, NpgsqlDbType.Integer);
                writer.Write(op.Timestamp, NpgsqlDbType.TimestampTz);
                writer.Write(op.OpHash, NpgsqlDbType.Char);
            }

            writer.Complete();
        }
        #endregion
    }

    public static class AttestationOperationModel
    {
        public static void BuildAttestationOperationModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<AttestationOperation>()
                .HasKey(x => x.Id);
            #endregion
            
            #region props
            modelBuilder.Entity<AttestationOperation>()
                .Property(x => x.OpHash)
                .IsFixedLength(true)
                .HasMaxLength(51)
                .IsRequired();
            #endregion

            #region indexes
            modelBuilder.Entity<AttestationOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<AttestationOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<AttestationOperation>()
                .HasIndex(x => x.DelegateId);
            #endregion
        }
    }
}
