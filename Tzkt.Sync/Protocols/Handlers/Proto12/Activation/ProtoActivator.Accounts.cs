using Newtonsoft.Json.Linq;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto12
{
    partial class ProtoActivator : Proto11.ProtoActivator
    {
        protected override async Task<List<Account>> BootstrapAccounts(Protocol protocol, JToken parameters)
        {
            var accounts = await base.BootstrapAccounts(protocol, parameters);

            Cache.Statistics.Current.TotalFrozen = accounts
                .Where(x => x is Data.Models.Delegate baker && baker.StakingBalance >= protocol.MinimalStake)
                .Sum(x => (x as Data.Models.Delegate).StakingBalance / (protocol.MaxDelegatedOverFrozenRatio + 1));

            return accounts;
        }
    }
}
