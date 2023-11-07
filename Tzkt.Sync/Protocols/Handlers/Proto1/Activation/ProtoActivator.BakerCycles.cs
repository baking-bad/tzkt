using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto1
{
    partial class ProtoActivator : ProtocolCommit
    {
        public virtual void BootstrapBakerCycles(
            Protocol protocol,
            List<Account> accounts,
            List<Cycle> cycles,
            List<IEnumerable<RightsGenerator.BR>> bakingRights,
            List<IEnumerable<RightsGenerator.ER>> endorsingRights)
        {
            var bakers = accounts
                .Where(x => x.Type == AccountType.Delegate)
                .Select(x => x as Data.Models.Delegate);

            var totalPower = bakers.Sum(x => x.StakingBalance - x.StakingBalance % protocol.MinimalStake);

            foreach (var cycle in cycles)
            {
                var bakerCycles = bakers.ToDictionary(x => x.Id, x =>
                {
                    var bakingPower = x.StakingBalance - x.StakingBalance % protocol.MinimalStake;
                    var share = (double)bakingPower / totalPower;
                    return new BakerCycle
                    {
                        Cycle = cycle.Index,
                        BakerId = x.Id,
                        OwnDelegatedBalance = x.Balance,
                        ExternalDelegatedBalance = x.DelegatedBalance,
                        DelegatorsCount = x.DelegatorsCount,
                        OwnStakedBalance = x.StakedBalance,
                        ExternalStakedBalance = x.ExternalStakedBalance,
                        StakersCount = x.StakersCount,
                        BakingPower = bakingPower,
                        TotalBakingPower = totalPower,
                        ExpectedBlocks = protocol.BlocksPerCycle * share, 
                        ExpectedEndorsements = protocol.EndorsersPerBlock * protocol.BlocksPerCycle * share
                    };
                });

                #region future baking rights
                foreach (var br in bakingRights[cycle.Index].SkipWhile(x => x.Level == 1)) // skip bootstrap block rights
                {
                    if (br.Round > 0)
                        continue;

                    if (!bakerCycles.TryGetValue(br.Baker, out var bakerCycle))
                        throw new Exception("Unknown baking right recipient");

                    bakerCycle.FutureBlocks++;
                    bakerCycle.FutureBlockRewards += GetFutureBlockReward(protocol, cycle.Index);
                }
                #endregion

                #region future endorsing rights
                var skipLevel = endorsingRights[cycle.Index].Last().Level; // skip shifted rights
                foreach (var er in endorsingRights[cycle.Index].TakeWhile(x => x.Level < skipLevel))
                {
                    if (!bakerCycles.TryGetValue(er.Baker, out var bakerCycle))
                        throw new Exception("Unknown endorsing right recipient");

                    bakerCycle.FutureEndorsements += er.Slots;
                    bakerCycle.FutureEndorsementRewards += GetFutureEndorsementReward(protocol, cycle.Index, er.Slots);
                }
                #endregion

                #region shifted future endirsing rights
                if (cycle.Index > 0)
                {
                    foreach (var er in endorsingRights[cycle.Index - 1].Reverse().TakeWhile(x => x.Level == cycle.FirstLevel - 1))
                    {
                        if (!bakerCycles.TryGetValue(er.Baker, out var bakerCycle))
                            throw new Exception("Unknown endorsing right recipient");

                        bakerCycle.FutureEndorsements += er.Slots;
                        bakerCycle.FutureEndorsementRewards += GetFutureEndorsementReward(protocol, cycle.Index, er.Slots);
                    }
                }
                #endregion

                Db.BakerCycles.AddRange(bakerCycles.Values);
            }
        }

        public async Task ClearBakerCycles()
        {
            await Db.Database.ExecuteSqlRawAsync(@"DELETE FROM ""BakerCycles""");
        }

        #region helpers
        protected virtual long GetFutureBlockReward(Protocol protocol, int cycle)
            => cycle < protocol.NoRewardCycles ? 0 : protocol.BlockReward0;

        protected virtual long GetFutureEndorsementReward(Protocol protocol, int cycle, int slots)
            => cycle < protocol.NoRewardCycles ? 0 : (slots * protocol.EndorsementReward0);
        #endregion
    }
}
