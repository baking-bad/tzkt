using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto1
{
    partial class ProtoActivator : ProtocolCommit
    {
        public void BootstrapDelegationSnapshots(List<Account> accounts)
        {
            Db.DelegationSnapshots.AddRange(accounts
                .Where(x => x.Staked)
                .Select(x => new DelegationSnapshot
                {
                    Level = 1,
                    BakerId = x.DelegateId ?? x.Id,
                    AccountId = x.Id,

                    OwnDelegatedBalance = x.Balance - ((x as Data.Models.Delegate)?.OwnStakedBalance ?? 0),
                    ExternalDelegatedBalance = (x as Data.Models.Delegate)?.DelegatedBalance,
                    DelegatorsCount = (x as Data.Models.Delegate)?.DelegatorsCount,

                    PrevMinTotalDelegatedLevel = (x as Data.Models.Delegate)?.MinTotalDelegatedLevel,
                    PrevMinTotalDelegated = (x as Data.Models.Delegate)?.MinTotalDelegated
                }));
        }

        public async Task ClearDelegationSnapshots()
        {
            await Db.Database.ExecuteSqlRawAsync("""
                DELETE FROM "DelegationSnapshots"
                """);
        }
    }
}
