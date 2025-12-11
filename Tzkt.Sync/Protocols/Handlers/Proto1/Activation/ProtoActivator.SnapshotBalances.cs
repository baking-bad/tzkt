using Microsoft.EntityFrameworkCore;
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
                    BakerId = x.DelegateId ?? x.Id,
                    AccountId = x.Id,

                    OwnDelegatedBalance = x.Balance - ((x as Data.Models.Delegate)?.OwnStakedBalance ?? 0),
                    ExternalDelegatedBalance = (x as Data.Models.Delegate)?.ExternalDelegatedBalance,
                    DelegatorsCount = (x as Data.Models.Delegate)?.DelegatorsCount,

                    OwnStakedBalance = (x as Data.Models.Delegate)?.OwnStakedBalance,
                    ExternalStakedBalance = (x as Data.Models.Delegate)?.ExternalStakedBalance,
                    StakersCount = (x as Data.Models.Delegate)?.StakersCount,

                    Pseudotokens = (x as Data.Models.Delegate)?.IssuedPseudotokens ?? (x as User)?.StakedPseudotokens
                }));
        }

        public async Task ClearSnapshotBalances()
        {
            await Db.Database.ExecuteSqlRawAsync("""
                DELETE FROM "SnapshotBalances"
                """);
        }
    }
}
