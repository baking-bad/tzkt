using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto12
{
    partial class ProtoActivator : Proto11.ProtoActivator
    {
        protected override async Task<(IEnumerable<RightsGenerator.BR>, IEnumerable<RightsGenerator.ER>)> GetRights(
            Protocol protocol,
            List<Account> accounts,
            Cycle cycle)
        {
            var bakers = accounts
                .Where(x => x is Data.Models.Delegate d && d.Balance > 0 && d.StakingBalance >= protocol.TokensPerRoll)
                .Select(x => x as Data.Models.Delegate);

            var sampler = GetSampler(bakers.Select(x => 
                (x.Id, Math.Min(x.StakingBalance, x.Balance * 100 / protocol.FrozenDepositsPercentage))));

            #region temporary diagnostics
            await sampler.Validate(Proto, 1, cycle.Index);
            #endregion

            var bakingRights = await RightsGenerator.GetBakingRightsAsync(sampler, protocol, cycle);
            var endorsingRights = await RightsGenerator.GetEndorsingRightsAsync(sampler, protocol, cycle);
            return (bakingRights, endorsingRights);
        }
    }
}
