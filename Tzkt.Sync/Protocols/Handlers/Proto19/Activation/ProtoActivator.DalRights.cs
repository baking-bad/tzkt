using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto19
{
    partial class ProtoActivator : Proto18.ProtoActivator
    {

        protected override async Task<IEnumerable<RightsGenerator.DR>> GetDalRights(Protocol protocol, List<Account> accounts, Cycle cycle)
        {
            var delegates = accounts
                .Where(x => x is Data.Models.Delegate d && d.Balance > 0 && d.StakingBalance >= protocol.MinimalStake)
                .Select(x => x as Data.Models.Delegate);

            var sampler = GetSampler(delegates.Select(x =>
                (x.Id, Math.Min(x.StakingBalance, x.Balance * (protocol.MaxDelegatedOverFrozenRatio + 1)))));

            #region temporary diagnostics
            await sampler.Validate(Proto, 1, cycle.Index);
            #endregion

            return await RightsGenerator.GetDalRightsAsync(sampler, protocol, cycle);
        }
    }
}
