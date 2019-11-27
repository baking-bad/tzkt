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
                manager.SystemOpsCount++;

                Db.SystemOps.Add(new SystemOperation
                {
                    Id = await Cache.NextCounterAsync(),
                    Level = state.Level,
                    Timestamp = state.Timestamp,
                    Account = manager,
                    Event = SystemEvent.AirDrop
                });
            }
        }

        public override async Task Revert()
        {
            var airDrops = await Db.SystemOps
                .AsNoTracking()
                .Include(x => x.Account)
                .Where(x => x.Event == SystemEvent.AirDrop)
                .ToListAsync();

            foreach (var airDrop in airDrops)
            {
                Db.TryAttach(airDrop.Account);
                Cache.AddAccount(airDrop.Account);

                airDrop.Account.Balance = 0;
                airDrop.Account.SystemOpsCount--;
            }

            Db.SystemOps.RemoveRange(airDrops);
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
