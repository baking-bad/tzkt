using System.Collections.Generic;
using System.Linq;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto12
{
    partial class ProtoActivator : Proto11.ProtoActivator
    {
        public override void BootstrapSnapshotBalances(List<Account> accounts)
        {
            Db.SnapshotBalances.AddRange(accounts.Where(x => x.Staked)
                .Select(x => new SnapshotBalance
                {
                    AccountId = x.Id, 
                    Balance = x.Balance,
                    DelegateId = x.DelegateId,
                    DelegatedBalance = (x as Data.Models.Delegate)?.DelegatedBalance,
                    DelegatorsCount = (x as Data.Models.Delegate)?.DelegatorsCount,
                    StakingBalance = (x as Data.Models.Delegate)?.StakingBalance,
                    Level = 1
                }));
        }
    }
}
