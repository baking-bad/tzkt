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
                    .Select(x =>
                    {
                        var stakedBalance = 0L;
                        if (x is User user && user.StakedPseudotokens != null)
                        {
                            var baker = Cache.Accounts.GetDelegate(x.DelegateId!.Value);
                            stakedBalance = (long)(baker.ExternalStakedBalance * user.StakedPseudotokens / baker.IssuedPseudotokens!);
                        }

                        return new DelegatorCycle
                        {
                            Id = 0,
                            Cycle = cycle,
                            DelegatorId = x.Id,
                            BakerId = x.DelegateId!.Value,
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
