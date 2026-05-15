using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tzkt.Data;
using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols
{
    public class BlockContext
    {
        public Block Block { get; set; } = null!;
        public Protocol Protocol { get; set; } = null!;

        #region operations
        public List<OriginationOperation> OriginationOps { get; set; } = [];
        public List<TransactionOperation> TransactionOps { get; set; } = [];
        public List<RevealOperation> RevealOps { get; set; } = [];
        public List<RegisterConstantOperation> RegisterConstantOps { get; set; } = [];
        public List<IncreasePaidStorageOperation> IncreasePaidStorageOps { get; set; } = [];
        public List<TransferTicketOperation> TransferTicketOps { get; set; } = [];
        #endregion

        #region fictive operations
        public List<MigrationOperation> MigrationOps { get; set; } = [];
        #endregion

        public IEnumerable<IOperation> EnumerateOps()
        {
            var ops = Enumerable.Empty<IOperation>();

            if (OriginationOps.Count != 0) ops = ops.Concat(OriginationOps);
            if (TransactionOps.Count != 0) ops = ops.Concat(TransactionOps);
            if (RevealOps.Count != 0) ops = ops.Concat(RevealOps);
            if (RegisterConstantOps.Count != 0) ops = ops.Concat(RegisterConstantOps);
            if (IncreasePaidStorageOps.Count != 0) ops = ops.Concat(IncreasePaidStorageOps);
            if (TransferTicketOps.Count != 0) ops = ops.Concat(TransferTicketOps);

            return ops;
        }

        public void Apply(TzktContext db)
        {
            var conn = (db.Database.GetDbConnection() as NpgsqlConnection)!;

            if (TransactionOps.Count != 0)
                TransactionOperation.Write(conn, TransactionOps);
        }

        public async Task Revert(TzktContext db)
        {
            if (TransactionOps.Count != 0)
                await db.Database.ExecuteSqlRawAsync($$"""
                    DELETE FROM "{{nameof(TzktContext.TransactionOps)}}"
                    WHERE "{{nameof(TransactionOperation.Level)}}" = {0}
                    """, Block.Level);
        }
    }
}
