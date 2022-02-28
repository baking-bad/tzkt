using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto1
{
    partial class ProtoActivator : ProtocolCommit
    {
        public void BootstrapBakerCycles(
            Protocol protocol,
            List<Account> accounts,
            List<JsonElement> bakingRights,
            List<JsonElement> endorsingRights)
        {
            var bakers = accounts
                .Where(x => x.Type == AccountType.Delegate)
                .Select(x => x as Data.Models.Delegate);

            var totalStaking = bakers.Sum(x => x.StakingBalance);

            for (int cycle = 0; cycle <= protocol.PreservedCycles; cycle++)
            {
                var bakerCycles = bakers.ToDictionary(x => x.Address, x =>
                {
                    var share = (double)x.StakingBalance / totalStaking;
                    return new BakerCycle
                    {
                        Cycle = cycle,
                        BakerId = x.Id,
                        StakingBalance = x.StakingBalance,
                        DelegatedBalance = x.DelegatedBalance,
                        DelegatorsCount = x.DelegatorsCount,
                        ExpectedBlocks = protocol.BlocksPerCycle * share, 
                        ExpectedEndorsements = protocol.EndorsersPerBlock * protocol.BlocksPerCycle * share
                    };
                });

                #region future baking rights
                var skipLevel = bakingRights[cycle][0].RequiredInt32("level"); // skip bootstrap block rights
                foreach (var br in bakingRights[cycle].EnumerateArray().SkipWhile(x => cycle == 0 && x.RequiredInt32("level") == skipLevel))
                {
                    if (br.RequiredInt32("priority") > 0)
                        continue;

                    if (!bakerCycles.TryGetValue(br.RequiredString("delegate"), out var bakerCycle))
                        throw new Exception("Unknown baking right recipient");

                    bakerCycle.FutureBlocks++;
                    bakerCycle.FutureBlockRewards += GetFutureBlockReward(protocol, cycle);
                }
                #endregion

                #region future endorsing rights
                skipLevel = endorsingRights[cycle].EnumerateArray().Last().RequiredInt32("level"); // skip shifted rights
                foreach (var er in endorsingRights[cycle].EnumerateArray().TakeWhile(x => x.RequiredInt32("level") < skipLevel))
                {
                    if (!bakerCycles.TryGetValue(er.RequiredString("delegate"), out var bakerCycle))
                        throw new Exception("Unknown endorsing right recipient");

                    var slots = er.RequiredArray("slots").Count();

                    bakerCycle.FutureEndorsements += slots;
                    bakerCycle.FutureEndorsementRewards += GetFutureEndorsementReward(protocol, cycle, slots);
                }
                #endregion

                #region shifted future endirsing rights
                if (cycle > 0)
                {
                    var shiftedLevel = endorsingRights[cycle - 1].EnumerateArray().Last().RequiredInt32("level");
                    foreach (var er in endorsingRights[cycle - 1].EnumerateArray().Reverse().TakeWhile(x => x.RequiredInt32("level") == shiftedLevel))
                    {
                        if (!bakerCycles.TryGetValue(er.RequiredString("delegate"), out var bakerCycle))
                            throw new Exception("Unknown endorsing right recipient");

                        var slots = er.RequiredArray("slots").Count();

                        bakerCycle.FutureEndorsements += slots;
                        bakerCycle.FutureEndorsementRewards += GetFutureEndorsementReward(protocol, cycle, slots);
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

        protected virtual long GetBlockDeposit(Protocol protocol, int cycle)
            => cycle < protocol.RampUpCycles
                ? (protocol.BlockDeposit / protocol.RampUpCycles * cycle)
                : protocol.BlockDeposit;

        protected virtual long GetFutureEndorsementReward(Protocol protocol, int cycle, int slots)
            => cycle < protocol.NoRewardCycles ? 0 : (slots * protocol.EndorsementReward0);

        protected virtual long GetEndorsementDeposit(Protocol protocol, int cycle, int slots)
            => cycle < protocol.RampUpCycles
                ? (slots * (protocol.EndorsementDeposit / protocol.RampUpCycles * cycle))
                : (slots * protocol.EndorsementDeposit);
        #endregion
    }
}
