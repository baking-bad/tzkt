using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto15
{
    partial class ProtoActivator : Proto14.ProtoActivator
    {
        public ProtoActivator(ProtocolHandler proto) : base(proto) { }

        protected override void SetParameters(Protocol protocol, JToken parameters)
        {
            base.SetParameters(protocol, parameters);
            protocol.TokensPerRoll = parameters["minimal_stake"]?.Value<long>() ?? 6_000_000L;
        }

        protected override void UpgradeParameters(Protocol protocol, Protocol prev)
        {
            if (Cache.AppState.GetChainId() == "NetXnHfVqm9iesp") // ghostnet
            {
            }
        }

        protected override async Task MigrateContext(AppState state)
        {
            await AddInvoice(state, "tz1X81bCXPtMiHu1d4UZF4GPhMPkvkp56ssb", 15_000_000_000L);
            await AddInvoice(state, "tz1MidLyXXvKWMmbRvKKeusDtP95NDJ5gAUx", 10_000_000_000L);

            if (state.ChainId == "NetXnHfVqm9iesp") // ghostnet
            {
            }
        }

        protected override async Task RevertContext(AppState state)
        {
            await RemoveInvoices(state);
        }

        async Task AddInvoice(AppState state, string address, long amount)
        {
            var block = await Cache.Blocks.CurrentAsync();
            var account = await Cache.Accounts.GetAsync(address);

            Db.TryAttach(account);
            account.FirstLevel = Math.Min(account.FirstLevel, state.Level);
            account.LastLevel = state.Level;
            account.Balance += amount;
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
                BalanceChange = amount
            });

            state.MigrationOpsCount++;

            var stats = await Cache.Statistics.GetAsync(state.Level);
            stats.TotalCreated += amount;
        }

        async Task RemoveInvoices(AppState state)
        {
            var block = await Cache.Blocks.CurrentAsync();

            var invoices = await Db.MigrationOps
                .AsNoTracking()
                .Include(x => x.Account)
                .Where(x => x.Level == block.Level && x.Kind == MigrationKind.ProposalInvoice)
                .ToListAsync();

            foreach (var invoice in invoices)
            {
                Db.TryAttach(invoice.Account);
                Cache.Accounts.Add(invoice.Account);

                invoice.Account.Balance -= invoice.BalanceChange;
                invoice.Account.MigrationsCount--;

                Db.MigrationOps.Remove(invoice);
                Cache.AppState.ReleaseOperationId();

                state.MigrationOpsCount--;
            }
        }
    }
}
