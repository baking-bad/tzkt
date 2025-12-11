using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto14
{
    partial class ProtoActivator : Proto13.ProtoActivator
    {
        public ProtoActivator(ProtocolHandler proto) : base(proto) { }

        protected override void SetParameters(Protocol protocol, JToken parameters)
        {
            base.SetParameters(protocol, parameters);
            protocol.Dictator = parameters["testnet_dictator"]?.Value<string>();
        }

        protected override void UpgradeParameters(Protocol protocol, Protocol prev)
        {
            if (Cache.AppState.GetChainId() == "NetXnHfVqm9iesp") // ghostnet
            {
                protocol.BlocksPerVoting = prev.BlocksPerCycle;
                protocol.Dictator = "tz1Xf8zdT3DbAX9cHw3c3CXh79rc4nK4gCe8"; // oxhead_testnet_baker
            }
        }

        protected override async Task MigrateContext(AppState state)
        {
            var block = await Cache.Blocks.CurrentAsync();
            Db.TryAttach(block);

            var account = (await Cache.Accounts.GetAsync("tz1X81bCXPtMiHu1d4UZF4GPhMPkvkp56ssb"))!;
            Db.TryAttach(account);
            Receive(account, 3_000_000_000L);
            account.FirstLevel = Math.Min(account.FirstLevel, state.Level);
            account.LastLevel = state.Level;
            account.MigrationsCount++;

            block.Operations |= Operations.Migrations;
            var migration = new MigrationOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Level = block.Level,
                Timestamp = block.Timestamp,
                AccountId = account.Id,
                Kind = MigrationKind.ProposalInvoice,
                BalanceChange = 3_000_000_000L
            };
            Db.MigrationOps.Add(migration);
            Context.MigrationOps.Add(migration);

            Db.TryAttach(state);
            state.MigrationOpsCount++;

            var stats = Cache.Statistics.Current;
            Db.TryAttach(stats);
            stats.TotalCreated += 3_000_000_000L;

            if (state.ChainId == "NetXnHfVqm9iesp") // ghostnet
            {
                var votingPeriod = await Cache.Periods.GetAsync(58);
                Db.TryAttach(votingPeriod);
                votingPeriod.LastLevel = 0;
                state.VotingPeriod = 58;
                state.VotingEpoch = 58;
            }
        }

        protected override async Task RevertContext(AppState state)
        {
            var block = await Cache.Blocks.CurrentAsync();

            var invoice = await Db.MigrationOps
                .AsNoTracking()
                .FirstAsync(x => x.Level == block.Level && x.Kind == MigrationKind.ProposalInvoice);

            var account = await Cache.Accounts.GetAsync(invoice.AccountId);
            Db.TryAttach(account);

            RevertReceive(account, invoice.BalanceChange);
            account.MigrationsCount--;

            Db.MigrationOps.Remove(invoice);
            Cache.AppState.ReleaseOperationId();

            state.MigrationOpsCount--;
        }
    }
}
