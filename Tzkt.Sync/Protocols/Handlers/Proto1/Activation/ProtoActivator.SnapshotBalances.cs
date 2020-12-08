using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto1
{
    partial class ProtoActivator : ProtocolCommit
    {
        public void BootstrapSnapshotBalances(List<Account> accounts)
        {
            Db.SnapshotBalances.AddRange(accounts.Where(x => x.Staked)
                .Select(x => new SnapshotBalance
                {
                    AccountId = x.Id,
                    Balance = x.Balance,
                    DelegateId = x.DelegateId,
                    Level = 1
                }));
        }

        public async Task ClearSnapshotBalances()
        {
            await Db.Database.ExecuteSqlRawAsync(@"DELETE FROM ""SnapshotBalances""");
        }
    }
}
