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
                Db.DelegatorCycles.AddRange(accounts.Where(x => x.DelegateId != null)
                    .Select(x =>
                    {
                        var stakedBalance = 0L;
                        if (x is User user && user.StakedPseudotokens != null)
                        {
                            var baker = Cache.Accounts.GetDelegate(user.DelegateId);
                            stakedBalance = (long)(baker.ExternalStakedBalance * user.StakedPseudotokens / baker.IssuedPseudotokens);
                        }

                        return new DelegatorCycle
                        {
                            Cycle = cycle,
                            DelegatorId = x.Id,
                            BakerId = (int)x.DelegateId,
                            DelegatedBalance = x.Balance,
                            StakedBalance = stakedBalance
                        };
                    }));
            }
        }

        public async Task ClearDelegatorCycles()
        {
            await Db.Database.ExecuteSqlRawAsync(@"DELETE FROM ""DelegatorCycles""");
        }
    }
}
