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
                    Level = 1,
                    Balance = x.Balance,
                    StakedBalance = (x as User)?.StakedBalance ?? 0,
                    AccountId = x.Id,
                    DelegateId = x.DelegateId,
                    StakingBalance = (x as Data.Models.Delegate)?.StakingBalance,
                    DelegatedBalance = (x as Data.Models.Delegate)?.DelegatedBalance,
                    DelegatorsCount = (x as Data.Models.Delegate)?.DelegatorsCount,
                    TotalStakedBalance = (x as Data.Models.Delegate)?.TotalStakedBalance,
                    ExternalStakedBalance = (x as Data.Models.Delegate)?.ExternalStakedBalance,
                    StakersCount = (x as Data.Models.Delegate)?.StakersCount,
                }));
        }

        public async Task ClearSnapshotBalances()
        {
            await Db.Database.ExecuteSqlRawAsync(@"DELETE FROM ""SnapshotBalances""");
        }
    }
}
