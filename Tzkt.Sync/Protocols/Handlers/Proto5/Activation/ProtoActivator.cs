using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto5
{
    class ProtoActivator : Proto4.ProtoActivator
    {
        public ProtoActivator(ProtocolHandler proto) : base(proto) { }

        protected override void SetParameters(Protocol protocol, JToken parameters)
        {
            base.SetParameters(protocol, parameters);
            protocol.BallotQuorumMin = parameters["quorum_min"]?.Value<int>() ?? 2000;
            protocol.BallotQuorumMax = parameters["quorum_max"]?.Value<int>() ?? 7000;
            protocol.ProposalQuorum = parameters["min_proposal_quorum"]?.Value<int>() ?? 500;
        }

        protected override void UpgradeParameters(Protocol protocol, Protocol prev)
        {
            base.UpgradeParameters(protocol, prev);
            protocol.BallotQuorumMin = 2000;
            protocol.BallotQuorumMax = 7000;
            protocol.ProposalQuorum = 500;
        }

        // Airdrop
        // Proposal invoice

        protected override async Task MigrateContext(AppState state)
        {
            var block = await Cache.Blocks.CurrentAsync();
            var statistics = await Cache.Statistics.GetAsync(state.Level);

            #region airdrop
            var emptiedManagers = await Db.Contracts
                .AsNoTracking()
                .Include(x => x.Manager)
                .Where(x => x.Spendable == null &&
                            x.Manager.Type == AccountType.User &&
                            x.Manager.Balance == 0 &&
                            x.Manager.Counter > 0)
                .Select(x => x.Manager)
                .ToListAsync();

            var dict = new Dictionary<string, User>(8000);
            foreach (var manager in emptiedManagers)
                dict[manager.Address] = manager as User;

            foreach (var manager in dict.Values)
            {
                Db.TryAttach(manager);
                Cache.Accounts.Add(manager);

                manager.Balance = 1;
                manager.Counter = state.ManagerCounter;
                manager.MigrationsCount++;

                block.Operations |= Operations.Migrations;
                Db.MigrationOps.Add(new MigrationOperation
                {
                    Id = Cache.AppState.NextOperationId(),
                    Block = block,
                    Level = state.Level,
                    Timestamp = state.Timestamp,
                    Account = manager,
                    Kind = MigrationKind.AirDrop,
                    BalanceChange = 1
                });
            }

            state.MigrationOpsCount += dict.Values.Count;
            statistics.TotalCreated += dict.Values.Count;
            #endregion

            #region invoice
            var account = await Cache.Accounts.GetAsync("KT1DUfaMfTRZZkvZAYQT5b3byXnvqoAykc43");

            Db.TryAttach(account);
            account.Balance += 500_000_000;
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
                BalanceChange = 500_000_000
            });

            state.MigrationOpsCount++;
            statistics.TotalCreated += 500_000_000;
            #endregion
        }

        protected override async Task RevertContext(AppState state)
        {
            #region airdrop
            var airDrops = await Db.MigrationOps
                .AsNoTracking()
                .Include(x => x.Account)
                .Where(x => x.Kind == MigrationKind.AirDrop)
                .ToListAsync();

            foreach (var airDrop in airDrops)
            {
                Db.TryAttach(airDrop.Account);
                Cache.Accounts.Add(airDrop.Account);

                airDrop.Account.Balance = 0;
                airDrop.Account.MigrationsCount--;
            }

            Db.MigrationOps.RemoveRange(airDrops);

            state.MigrationOpsCount -= airDrops.Count;
            #endregion

            #region invoice
            var invoice = await Db.MigrationOps
                .AsNoTracking()
                .Include(x => x.Account)
                .FirstOrDefaultAsync(x => x.Level == state.Level && x.Kind == MigrationKind.ProposalInvoice);

            Db.TryAttach(invoice.Account);
            Cache.Accounts.Add(invoice.Account);

            invoice.Account.Balance -= 500_000_000;
            invoice.Account.MigrationsCount--;

            Db.MigrationOps.Remove(invoice);

            state.MigrationOpsCount--;
            #endregion
        }
    }
}
