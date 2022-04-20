using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

            var selectedStake = bakers.Sum(x => x.StakingBalance - x.StakingBalance % protocol.TokensPerRoll);

            for (int cycle = 0; cycle <= protocol.PreservedCycles; cycle++)
            {
                var bakerCycles = bakers.ToDictionary(x => x.Id, x =>
                {
                    var activeStake = x.StakingBalance - x.StakingBalance % protocol.TokensPerRoll;
                    var share = (double)activeStake / selectedStake;
                    return new BakerCycle
                    {
                        Cycle = cycle,
                        BakerId = x.Id,
                        StakingBalance = x.StakingBalance,
                        ActiveStake = activeStake,
                        SelectedStake = selectedStake,
                        DelegatedBalance = x.DelegatedBalance,
                        DelegatorsCount = x.DelegatorsCount,
                        ExpectedBlocks = protocol.BlocksPerCycle * share, 
                        ExpectedEndorsements = protocol.EndorsersPerBlock * protocol.BlocksPerCycle * share
                    };
                });

                #region future baking rights
                foreach (var br in bakingRights[cycle].SkipWhile(x => x.Level == 1)) // skip bootstrap block rights
                {
                    if (br.Round > 0)
                        continue;

                    if (!bakerCycles.TryGetValue(br.Baker, out var bakerCycle))
                        throw new Exception("Unknown baking right recipient");

                    bakerCycle.FutureBlocks++;
                    bakerCycle.FutureBlockRewards += GetFutureBlockReward(protocol, cycle);
                }
                #endregion

                #region future endorsing rights
                var skipLevel = endorsingRights[cycle].Last().Level; // skip shifted rights
                foreach (var er in endorsingRights[cycle].TakeWhile(x => x.Level < skipLevel))
                {
                    if (!bakerCycles.TryGetValue(er.Baker, out var bakerCycle))
                        throw new Exception("Unknown endorsing right recipient");

                    bakerCycle.FutureEndorsements += er.Slots;
                    bakerCycle.FutureEndorsementRewards += GetFutureEndorsementReward(protocol, cycle, er.Slots);
                }
                #endregion

                #region shifted future endirsing rights
                if (cycle > 0)
                {
                    var shiftedLevel = endorsingRights[cycle - 1].Last().Level;
                    foreach (var er in endorsingRights[cycle - 1].Reverse().TakeWhile(x => x.Level == shiftedLevel))
                    {
                        if (!bakerCycles.TryGetValue(er.Baker, out var bakerCycle))
                            throw new Exception("Unknown endorsing right recipient");

                        bakerCycle.FutureEndorsements += er.Slots;
                        bakerCycle.FutureEndorsementRewards += GetFutureEndorsementReward(protocol, cycle, er.Slots);
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
