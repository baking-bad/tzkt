using System.Collections.Generic;
using System.Threading.Tasks;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto12
{
    partial class ProtoActivator : Proto11.ProtoActivator
    {
        protected override async Task<(IEnumerable<RightsGenerator.BR>, IEnumerable<RightsGenerator.ER>)> GetRights(Protocol protocol, Cycle cycle)
        {
            var sampler = await Sampler.CreateAsync(Proto, cycle.Index);
            var bakingRights = await RightsGenerator.GetBakingRightsAsync(sampler, protocol, cycle);
            var endorsingRights = await RightsGenerator.GetEndorsingRightsAsync(sampler, protocol, cycle);
            return (bakingRights, endorsingRights);
        }
    }
}
