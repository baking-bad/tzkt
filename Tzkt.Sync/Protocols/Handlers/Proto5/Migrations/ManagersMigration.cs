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
            var emptiedManagers = await Db.Contracts
                .AsNoTracking()
                .Include(x => x.Manager)
                .Where(x => x.Kind == ContractKind.DelegatorContract &&
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
                manager.AirDrop = true;
            }
        }

        public override async Task Revert()
        {
            var airDropManagers = await Db.Users
                .AsNoTracking()
                .Where(x => x.AirDrop == true)
                .ToListAsync();

            foreach (var manager in airDropManagers)
            {
                Db.TryAttach(manager);
                Cache.AddAccount(manager);

                manager.Balance = 0;
                manager.AirDrop = null;
            }
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
