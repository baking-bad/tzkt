using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto5
{
    class ManagersMigration : ProtocolCommit
    {
        ManagersMigration(ProtocolHandler protocol) : base(protocol) { }

        public override async Task Apply()
        {
            var block = await Cache.GetCurrentBlockAsync();
            var state = await Cache.GetAppStateAsync();

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
                Cache.AddAccount(manager);

                manager.Balance = 1;
                manager.Counter = state.ManagerCounter;
                manager.MigrationsCount++;

                block.Operations |= Operations.Migrations;
                Db.MigrationOps.Add(new MigrationOperation
                {
                    Id = await Cache.NextCounterAsync(),
                    Block = block,
                    Level = state.Level,
                    Timestamp = state.Timestamp,
                    Account = manager,
                    Kind = MigrationKind.AirDrop,
                    BalanceChange = 1
                });
            }
        }

        public override async Task Revert()
        {
            var airDrops = await Db.MigrationOps
                .AsNoTracking()
                .Include(x => x.Account)
                .Where(x => x.Kind == MigrationKind.AirDrop)
                .ToListAsync();

            foreach (var airDrop in airDrops)
            {
                Db.TryAttach(airDrop.Account);
                Cache.AddAccount(airDrop.Account);

                airDrop.Account.Balance = 0;
                airDrop.Account.MigrationsCount--;
            }

            Db.MigrationOps.RemoveRange(airDrops);
        }

        #region static
        public static async Task<ManagersMigration> Apply(ProtocolHandler proto)
        {
            var commit = new ManagersMigration(proto);
            await commit.Apply();

            return commit;
        }

        public static async Task<ManagersMigration> Revert(ProtocolHandler proto)
        {
            var commit = new ManagersMigration(proto);
            await commit.Revert();

            return commit;
        }
        #endregion
    }
}
