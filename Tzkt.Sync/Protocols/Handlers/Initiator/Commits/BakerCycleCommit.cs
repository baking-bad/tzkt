using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Initiator
{
    class BakerCycleCommit : ProtocolCommit
    {
        public Block Block { get; private set; }
        public List<Account> BootstrapedAccounts { get; private set; }
        public List<List<RawBakingRight>> FutureBakingRights { get; private set; }
        public List<List<RawEndorsingRight>> FutureEndorsingRights { get; private set; }

        BakerCycleCommit(ProtocolHandler protocol) : base(protocol) { }

        public override Task Apply()
        {
            var bakers = BootstrapedAccounts
                .Where(x => x.Type == AccountType.Delegate)
                .Select(x => x as Data.Models.Delegate);

            var totalRolls = bakers.Sum(x => (int)(x.StakingBalance / Block.Protocol.TokensPerRoll));

            for (int cycle = 0; cycle <= Block.Protocol.PreservedCycles; cycle++)
            {
                var bakerCycles = bakers.ToDictionary(x => x.Address, x =>
                {
                    var rolls = (int)(x.StakingBalance / Block.Protocol.TokensPerRoll);
                    var rollsShare = (double)rolls / totalRolls;

                    var bakerCycle = new BakerCycle
                    {
                        Cycle = cycle,
                        BakerId = x.Id,
                        Rolls = rolls,
                        StakingBalance = x.StakingBalance,
                        DelegatedBalance = x.StakingBalance - x.Balance, //nothing is frozen yet
                        DelegatorsCount = x.DelegatorsCount,
                        ExpectedBlocks = Block.Protocol.BlocksPerCycle * rollsShare, 
                        ExpectedEndorsements = Block.Protocol.EndorsersPerBlock * Block.Protocol.BlocksPerCycle * rollsShare
                    };

                    return bakerCycle;
                });

                #region future baking rights
                var skipLevel = FutureBakingRights[cycle][0].Level; //skip bootstrap block rights
                foreach (var br in FutureBakingRights[cycle].SkipWhile(x => cycle == 0 && x.Level == skipLevel))
                {
                    if (br.Priority > 0)
                        continue;

                    if (!bakerCycles.TryGetValue(br.Delegate, out var bakerCycle))
                        throw new Exception("Unknown baking right recipient");

                    bakerCycle.FutureBlocks++;
                    bakerCycle.FutureBlockDeposits += GetBlockDeposit(Block.Protocol, cycle);
                    bakerCycle.FutureBlockRewards += GetFutureBlockReward(Block.Protocol, cycle);
                }
                #endregion

                #region future endorsing rights
                skipLevel = FutureEndorsingRights[cycle][^1].Level; //skip shifted rights
                foreach (var er in FutureEndorsingRights[cycle].TakeWhile(x => x.Level < skipLevel))
                {
                    if (!bakerCycles.TryGetValue(er.Delegate, out var bakerCycle))
                        throw new Exception("Unknown endorsing right recipient");

                    bakerCycle.FutureEndorsements += er.Slots.Count;
                    bakerCycle.FutureEndorsementDeposits += GetEndorsementDeposit(Block.Protocol, cycle, er.Slots.Count);
                    bakerCycle.FutureEndorsementRewards += GetFutureEndorsementReward(Block.Protocol, cycle, er.Slots.Count);
                }
                #endregion

                #region shifted future endirsing rights
                if (cycle > 0)
                {
                    var shiftedLevel = FutureEndorsingRights[cycle - 1][^1].Level;
                    foreach (var er in FutureEndorsingRights[cycle - 1].AsEnumerable().Reverse().TakeWhile(x => x.Level == shiftedLevel))
                    {
                        if (!bakerCycles.TryGetValue(er.Delegate, out var bakerCycle))
                            throw new Exception("Unknown endorsing right recipient");

                        bakerCycle.FutureEndorsements += er.Slots.Count;
                        bakerCycle.FutureEndorsementDeposits += GetEndorsementDeposit(Block.Protocol, cycle, er.Slots.Count);
                        bakerCycle.FutureEndorsementRewards += GetFutureEndorsementReward(Block.Protocol, cycle, er.Slots.Count);
                    }
                }
                #endregion

                Db.BakerCycles.AddRange(bakerCycles.Values);
            }

            return Task.CompletedTask;
        }

        public override async Task Revert()
        {
            await Db.Database.ExecuteSqlRawAsync(@"DELETE FROM ""BakerCycles""");
        }

        #region helpers
        //TODO: figure out how to avoid hardcoded constants for future cycles

        long GetFutureBlockReward(Protocol protocol, int cycle)
            => protocol.BlockReward0 == 0 && cycle < protocol.PreservedCycles + 2 ? 0 : protocol.BlockReward0 * (protocol.BlockReward1 == 0 ? 1: protocol.EndorsersPerBlock); //TODO: use protocol_parameters

        long GetBlockDeposit(Protocol protocol, int cycle)
            => protocol.BlockDeposit < 512_000_000L && cycle < 64 ? cycle * 8_000_000L : 512_000_000L; //TODO: use protocol_parameters

        long GetFutureEndorsementReward(Protocol protocol, int cycle, int slots)
            => protocol.EndorsementReward0 == 0 && cycle < protocol.PreservedCycles + 2 ? 0 : slots * protocol.EndorsementReward0; //TODO: use protocol_parameters

        long GetEndorsementDeposit(Protocol protocol, int cycle, int slots)
            => slots * (protocol.EndorsementDeposit < 64_000_000L && cycle < 64 ? cycle * 1_000_000L : 64_000_000L); //TODO: use protocol_parameters
        #endregion

        #region static
        public static async Task<BakerCycleCommit> Apply(
            ProtocolHandler proto,
            Block block,
            List<Account> accounts,
            List<List<RawBakingRight>> bakingRights,
            List<List<RawEndorsingRight>> endorsingRights)
        {
            var commit = new BakerCycleCommit(proto)
            {
                Block = block,
                BootstrapedAccounts = accounts,
                FutureBakingRights = bakingRights,
                FutureEndorsingRights = endorsingRights
            };
            await commit.Apply();
            return commit;
        }

        public static async Task<BakerCycleCommit> Revert(ProtocolHandler proto)
        {
            var commit = new BakerCycleCommit(proto);
            await commit.Revert();
            return commit;
        }
        #endregion
    }
}
