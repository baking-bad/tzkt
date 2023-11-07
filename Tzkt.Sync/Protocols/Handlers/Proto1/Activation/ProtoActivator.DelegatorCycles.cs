using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto1
{
    partial class ProtoActivator : ProtocolCommit
    {
        public void BootstrapDelegatorCycles(Protocol protocol, List<Account> accounts)
        {
            for (int cycle = 0; cycle <= protocol.PreservedCycles; cycle++)
            {
                Db.DelegatorCycles.AddRange(accounts.Where(x => x.DelegateId != null)
                    .Select(x => new DelegatorCycle
                    {
                        Cycle = cycle,
                        DelegatorId = x.Id,
                        BakerId = (int)x.DelegateId,
                        DelegatedBalance = x.Balance,
                        StakedBalance = (x as User)?.StakedBalance ?? 0
                    }));
            }
        }

        public async Task ClearDelegatorCycles()
        {
            await Db.Database.ExecuteSqlRawAsync(@"DELETE FROM ""DelegatorCycles""");
        }
    }
}
