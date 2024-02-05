using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto12
{
    partial class ProtoActivator : Proto11.ProtoActivator
    {
        public override void BootstrapBakerCycles(
            Protocol protocol,
            List<Account> accounts,
            List<Cycle> cycles,
            List<IEnumerable<RightsGenerator.BR>> bakingRights,
            List<IEnumerable<RightsGenerator.ER>> endorsingRights)
        {
            var bakers = accounts
                .Where(x => x.Type == AccountType.Delegate)
                .Select(x => x as Data.Models.Delegate);

            foreach (var cycle in cycles)
            {
                var bakerCycles = bakers.ToDictionary(x => x.Id, x =>
                {
                    var bakerCycle = new BakerCycle
                    {
                        Cycle = cycle.Index,
                        BakerId = x.Id,
                        OwnDelegatedBalance = x.Balance,
                        ExternalDelegatedBalance = x.DelegatedBalance,
                        DelegatorsCount = x.DelegatorsCount,
                        OwnStakedBalance = x.StakedBalance,
                        ExternalStakedBalance = x.ExternalStakedBalance,
                        StakersCount = x.StakersCount,
                        BakingPower = 0,
                        TotalBakingPower = cycle.TotalBakingPower
                    };
                    if (x.StakingBalance >= protocol.MinimalStake)
                    {
                        var bakingPower = Math.Min(x.StakingBalance, x.Balance * (protocol.MaxDelegatedOverFrozenRatio + 1));
                        var expectedEndorsements = (int)(new BigInteger(protocol.BlocksPerCycle) * protocol.EndorsersPerBlock * bakingPower / cycle.TotalBakingPower);
                        bakerCycle.BakingPower = bakingPower;
                        bakerCycle.ExpectedBlocks = protocol.BlocksPerCycle * bakingPower / cycle.TotalBakingPower;
                        bakerCycle.ExpectedEndorsements = expectedEndorsements;
                        bakerCycle.FutureEndorsementRewards = expectedEndorsements * protocol.EndorsementReward0;
                    }
                    return bakerCycle;
                });

                #region future baking rights
                foreach (var br in bakingRights[cycle.Index].SkipWhile(x => x.Level == 1).Where(x => x.Round == 0))
                {
                    if (!bakerCycles.TryGetValue(br.Baker, out var bakerCycle))
                        throw new Exception("Unknown baking right recipient");

                    bakerCycle.FutureBlocks++;
                    bakerCycle.FutureBlockRewards += protocol.MaxBakingReward;
                }
                #endregion

                #region future endorsing rights
                foreach (var er in endorsingRights[cycle.Index].TakeWhile(x => x.Level < cycle.LastLevel))
                {
                    if (!bakerCycles.TryGetValue(er.Baker, out var bakerCycle))
                        throw new Exception("Unknown endorsing right recipient");

                    bakerCycle.FutureEndorsements += er.Slots;
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
                    }
                }
                #endregion

                Db.BakerCycles.AddRange(bakerCycles.Values);
            }
        }
    }
}
