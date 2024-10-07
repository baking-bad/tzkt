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
                .Select(x =>
                {
                    var ownStakedBalance = 0L;
                    if (x is Data.Models.Delegate baker)
                    {
                        ownStakedBalance = baker.OwnStakedBalance;
                    }
                    else if (x is User user && user.StakedPseudotokens != null)
                    {
                        baker = Cache.Accounts.GetDelegate(user.DelegateId);
                        ownStakedBalance = (long)(baker.ExternalStakedBalance * user.StakedPseudotokens / baker.IssuedPseudotokens);
                    }

                    return new SnapshotBalance
                    {
                        Level = 1,
                        AccountId = x.Id,
                        BakerId = x.DelegateId ?? x.Id,

                        OwnDelegatedBalance = x.Balance - ((x as Data.Models.Delegate)?.OwnStakedBalance ?? 0),
                        ExternalDelegatedBalance = (x as Data.Models.Delegate)?.DelegatedBalance ?? 0,
                        DelegatorsCount = (x as Data.Models.Delegate)?.DelegatorsCount ?? 0,

                        OwnStakedBalance = ownStakedBalance,
                        ExternalStakedBalance = (x as Data.Models.Delegate)?.ExternalStakedBalance ?? 0,
                        StakersCount = (x as Data.Models.Delegate)?.StakersCount ?? 0
                    };
                }));
        }

        public async Task ClearSnapshotBalances()
        {
            await Db.Database.ExecuteSqlRawAsync($@"DELETE FROM ""{nameof(TzktContext.SnapshotBalances)}""");
        }
    }
}
