using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto1
{
    partial class ProtoActivator : ProtocolCommit
    {
        public void BootstrapDelegatorCycles(Protocol protocol, List<Account> accounts)
        {
            for (int cycle = 0; cycle <= protocol.ConsensusRightsDelay; cycle++)
            {
                Db.DelegatorCycles.AddRange(accounts
                    .Where(x => x.DelegateId != null)
                    .Select(x => new DelegatorCycle
                    {
                        Id = 0,
                        Cycle = cycle,
                        DelegatorId = x.Id,
                        BakerId = x.DelegateId!.Value,
                        DelegatedBalance = x.Balance,
                        StakedPseudotokens = (x as User)?.StakedPseudotokens
                    }));
            }
        }

        public async Task ClearDelegatorCycles()
        {
            await Db.Database.ExecuteSqlRawAsync("""
                DELETE FROM "DelegatorCycles"
                """);
        }
    }
}
