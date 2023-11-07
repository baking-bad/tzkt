using Microsoft.EntityFrameworkCore;
using Tzkt.Data;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto1
{
    partial class ProtoActivator : ProtocolCommit
    {
        public void BootstrapSnapshotBalances(List<Account> accounts)
        {
            Db.SnapshotBalances.AddRange(accounts
                .Where(x => x.Staked)
                .Select(x => new SnapshotBalance
                {
                    Level = 1,
                    AccountId = x.Id,

                    BakerId = x.DelegateId ?? x.Id,
                    
                    OwnDelegatedBalance = x.Balance,
                    ExternalDelegatedBalance = (x as Data.Models.Delegate)?.DelegatedBalance ?? 0,
                    DelegatorsCount = (x as Data.Models.Delegate)?.DelegatorsCount ?? 0,
                    
                    OwnStakedBalance = (x as User)?.StakedBalance ?? 0,
                    ExternalStakedBalance = (x as Data.Models.Delegate)?.ExternalStakedBalance ?? 0,
                    StakersCount = (x as Data.Models.Delegate)?.StakersCount ?? 0,

                    StakedPseudotokens = (x as User)?.StakedPseudotokens ?? 0,
                    IssuedPseudotokens = (x as Data.Models.Delegate)?.IssuedPseudotokens ?? 0
                }));
        }

        public async Task ClearSnapshotBalances()
        {
            await Db.Database.ExecuteSqlRawAsync($@"DELETE FROM ""{nameof(TzktContext.SnapshotBalances)}""");
        }
    }
}
