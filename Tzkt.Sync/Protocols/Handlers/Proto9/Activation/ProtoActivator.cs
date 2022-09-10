using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto9
{
    class ProtoActivator : Proto8.ProtoActivator
    {
        public ProtoActivator(ProtocolHandler proto) : base(proto) { }

        // Proposal invoice

        protected override async Task MigrateContext(AppState state)
        {
            var block = await Cache.Blocks.CurrentAsync();
            var account = await Cache.Accounts.GetAsync("tz1abmz7jiCV2GH2u81LRrGgAFFgvQgiDiaf");

            Db.TryAttach(account);
            account.Balance += 100_000_000;
            if (account is Delegate delegat)
                delegat.StakingBalance += 100_000_000;
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

            state.MigrationOpsCount++;

            var stats = await Cache.Statistics.GetAsync(state.Level);
            stats.TotalCreated += 100_000_000;
        }

        protected override async Task RevertContext(AppState state)
        {
            var block = await Cache.Blocks.CurrentAsync();

            var invoice = await Db.MigrationOps
                .AsNoTracking()
                .Include(x => x.Account)
                .FirstOrDefaultAsync(x => x.Level == block.Level && x.Kind == MigrationKind.ProposalInvoice);

            Db.TryAttach(invoice.Account);
            Cache.Accounts.Add(invoice.Account);

            invoice.Account.Balance -= 100_000_000;
            if (invoice.Account is Delegate delegat)
                delegat.StakingBalance -= 100_000_000;
            invoice.Account.MigrationsCount--;

            Db.MigrationOps.Remove(invoice);
            Cache.AppState.ReleaseOperationId();

            state.MigrationOpsCount--;
        }
    }
}
