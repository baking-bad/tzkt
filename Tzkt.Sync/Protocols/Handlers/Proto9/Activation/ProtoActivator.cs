using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto9
{
    class ProtoActivator(ProtocolHandler proto) : Proto8.ProtoActivator(proto)
    {
        // Proposal invoice

        protected override async Task MigrateContext(AppState state)
        {
            var block = await Cache.Blocks.CurrentAsync();
            Db.TryAttach(block);

            var account = (await Cache.Accounts.GetAsync("tz1abmz7jiCV2GH2u81LRrGgAFFgvQgiDiaf"))!;
            Db.TryAttach(account);
            Receive(account, 100_000_000);
            account.MigrationsCount++;
            account.LastLevel = block.Level;

            block.Operations |= Operations.Migrations;

            var migration = new MigrationOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Level = block.Level,
                Timestamp = block.Timestamp,
                AccountId = account.Id,
                Kind = MigrationKind.ProposalInvoice,
                BalanceChange = 100_000_000
            };
            Db.MigrationOps.Add(migration);
            Context.MigrationOps.Add(migration);

            Db.TryAttach(state);
            state.MigrationOpsCount++;

            var stats = Cache.Statistics.Current;
            Db.TryAttach(stats);
            stats.TotalCreated += 100_000_000;
        }

        protected override async Task RevertContext(AppState state)
        {
            var block = await Cache.Blocks.CurrentAsync();

            var invoice = await Db.MigrationOps
                .AsNoTracking()
                .FirstAsync(x => x.Level == block.Level && x.Kind == MigrationKind.ProposalInvoice);

            var account = await Cache.Accounts.GetAsync(invoice.AccountId);
            Db.TryAttach(account);
            RevertReceive(account, 100_000_000);
            account.MigrationsCount--;

            Db.MigrationOps.Remove(invoice);
            Cache.AppState.ReleaseOperationId();

            state.MigrationOpsCount--;
        }
    }
}
