using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;

namespace Tzkt.Data.Models
{
    public class BigMapUpdate
    {
        public required int Id { get; set; }
        public required int BigMapPtr { get; set; }
        public required int Level { get; set; }
        public required BigMapAction Action { get; set; }

        public long? OriginationId { get; set; }
        public long? TransactionId { get; set; }
        public long? MigrationId { get; set; }

        public int? BigMapKeyId { get; set; }
        public byte[]? RawValue { get; set; }
        public string? JsonValue { get; set; }

        #region binary writer
        public static void Write(NpgsqlConnection conn, IEnumerable<BigMapUpdate> updates)
        {
            using var writer = conn.BeginBinaryImport($"""
                COPY "{nameof(TzktContext.BigMapUpdates)}" (
                    "{nameof(Id)}",
                    "{nameof(BigMapPtr)}",
                    "{nameof(Level)}",
                    "{nameof(Action)}",
                    "{nameof(OriginationId)}",
                    "{nameof(TransactionId)}",
                    "{nameof(MigrationId)}",
                    "{nameof(BigMapKeyId)}",
                    "{nameof(RawValue)}",
                    "{nameof(JsonValue)}"
                )
                FROM STDIN (FORMAT BINARY)
                """);

            foreach (var update in updates)
            {
                writer.StartRow();

                writer.Write(update.Id, NpgsqlDbType.Integer);
                writer.Write(update.BigMapPtr, NpgsqlDbType.Integer);
                writer.Write(update.Level, NpgsqlDbType.Integer);
                writer.Write((int)update.Action, NpgsqlDbType.Integer);
                writer.WriteNullable(update.OriginationId, NpgsqlDbType.Bigint);
                writer.WriteNullable(update.TransactionId, NpgsqlDbType.Bigint);
                writer.WriteNullable(update.MigrationId, NpgsqlDbType.Bigint);
                writer.WriteNullable(update.BigMapKeyId, NpgsqlDbType.Integer);
                writer.WriteNullable(update.RawValue, NpgsqlDbType.Bytea);
                writer.WriteNullable(update.JsonValue, NpgsqlDbType.Jsonb);
            }

            writer.Complete();
        }
        #endregion
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
