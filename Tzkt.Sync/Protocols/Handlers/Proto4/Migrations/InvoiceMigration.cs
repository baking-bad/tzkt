using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto4
{
    class InvoiceMigration : ProtocolCommit
    {
        InvoiceMigration(ProtocolHandler protocol) : base(protocol) { }

        public override async Task Apply()
        {
            var block = await Cache.Blocks.CurrentAsync();
            var account = await Cache.Accounts.GetAsync("tz1iSQEcaGpUn6EW5uAy3XhPiNg7BHMnRSXi");

            Db.TryAttach(account);
            account.Balance += 100_000_000;
            account.MigrationsCount++;

            block.Operations |= Operations.Migrations;
            Db.MigrationOps.Add(new MigrationOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Block = block,
                Level = block.Level,
                Timestamp = block.Timestamp,
                Account = account,
                Kind = MigrationKind.ProposalInvoice,
                BalanceChange = 100_000_000
            });

            var state = Cache.AppState.Get();
            state.MigrationOpsCount++;
            state.TotalCreated += 100_000_000;
        }

        public override async Task Revert()
        {
            var block = await Cache.Blocks.CurrentAsync();

            var invoice = await Db.MigrationOps
                .AsNoTracking()
                .Include(x => x.Account)
                .FirstOrDefaultAsync(x => x.Level == block.Level && x.Kind == MigrationKind.ProposalInvoice);

            Db.TryAttach(invoice.Account);
            Cache.Accounts.Add(invoice.Account);

            invoice.Account.Balance -= 100_000_000;
            invoice.Account.MigrationsCount--;

            Db.MigrationOps.Remove(invoice);

            var state = Cache.AppState.Get();
            Db.TryAttach(state);
            state.MigrationOpsCount--;
            state.TotalCreated -= 100_000_000;
        }

        #region static
        public static async Task<InvoiceMigration> Apply(ProtocolHandler proto)
        {
            var commit = new InvoiceMigration(proto);
            await commit.Apply();

            return commit;
        }

        public static async Task<InvoiceMigration> Revert(ProtocolHandler proto)
        {
            var commit = new InvoiceMigration(proto);
            await commit.Revert();

            return commit;
        }
        #endregion
    }
}
