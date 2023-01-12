using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto4
{
    class ProtoActivator : Proto3.ProtoActivator
    {
        public ProtoActivator(ProtocolHandler proto) : base(proto) { }

        protected override void SetParameters(Protocol protocol, JToken parameters)
        {
            base.SetParameters(protocol, parameters);
            protocol.HardBlockGasLimit = parameters["hard_gas_limit_per_block"]?.Value<int>() ?? 8_000_000;
            protocol.HardOperationGasLimit = parameters["hard_gas_limit_per_operation"]?.Value<int>() ?? 800_000;
            protocol.TokensPerRoll = parameters["tokens_per_roll"]?.Value<long>() ?? 8_000_000_000;
        }

        protected override void UpgradeParameters(Protocol protocol, Protocol prev)
        {
            protocol.HardBlockGasLimit = 8_000_000;
            protocol.HardOperationGasLimit = 800_000;
            protocol.TokensPerRoll = 8_000_000_000;
        }

        // Proposal invoice

        protected override async Task MigrateContext(AppState state)
        {
            var block = await Cache.Blocks.CurrentAsync();
            var account = await Cache.Accounts.GetAsync("tz1iSQEcaGpUn6EW5uAy3XhPiNg7BHMnRSXi");

            Db.TryAttach(account);
            account.FirstLevel = account.LastLevel = state.Level;
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
            invoice.Account.MigrationsCount--;

            Db.MigrationOps.Remove(invoice);
            Cache.AppState.ReleaseOperationId();

            state.MigrationOpsCount--;
        }
    }
}
