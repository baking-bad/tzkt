using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto4
{
    class BakerCycleCommit : Proto1.BakerCycleCommit
    {
        public BakerCycleCommit(ProtocolHandler protocol) : base(protocol) { }

        public override async Task Apply(
            Block block,
            Cycle futureCycle,
            IEnumerable<JsonElement> futureBakingRights,
            IEnumerable<JsonElement> futureEndorsingRights,
            Dictionary<int, Proto1.CycleCommit.DelegateSnapshot> snapshots,
            List<BakingRight> currentRights)
        {
            #region current rights
            var prevBlock = await Cache.Blocks.CurrentAsync();
            var prevBakingRights = prevBlock.Level == 1 ? new List<BakingRight>(0)
                : await Cache.BakingRights.GetAsync(prevBlock.Cycle, prevBlock.Level);

            foreach (var rights in currentRights.GroupBy(x => x.BakerId))
            {
                var bakerCycle = await Cache.BakerCycles.GetAsync(block.Cycle, rights.Key);
                Db.TryAttach(bakerCycle);

                var bakingRights = rights
                    .Where(x => x.Type == BakingRightType.Baking)
                    .OrderBy(x => x.Priority)
                    .ToList();

                var endorsingRight = rights
                    .FirstOrDefault(x => x.Type == BakingRightType.Endorsing);

                #region rights and deposits
                foreach (var br in bakingRights)
                {
                    if (br.Priority == 0)
                    {
                        bakerCycle.FutureBlocks--;
                        bakerCycle.FutureBlockDeposits -= GetBlockDeposit(block.Protocol, block.Cycle);
                    }

                    if (br.Status == BakingRightStatus.Realized)
                    {
                        bakerCycle.BlockDeposits += GetBlockDeposit(block.Protocol, block.Cycle);

                        if (br.Priority == 0)
                            bakerCycle.OwnBlocks++;
                        else
                            bakerCycle.ExtraBlocks++;
                    }
                    else if (br.Status == BakingRightStatus.Uncovered)
                    {
                        if (br.Priority == 0)
                            bakerCycle.UncoveredOwnBlocks++;
                        else
                            bakerCycle.UncoveredExtraBlocks++;
                    }
                    else if (br.Status == BakingRightStatus.Missed)
                    {
                        if (br.Priority == 0)
                            bakerCycle.MissedOwnBlocks++;
                        else
                            bakerCycle.MissedExtraBlocks++;
                    }
                    else
                    {
                        throw new Exception("Unexpected future rights");
                    }
                }

                if (endorsingRight != null)
                {
                    bakerCycle.FutureEndorsements -= (int)endorsingRight.Slots;
                    bakerCycle.FutureEndorsementDeposits -= GetEndorsementDeposit(block.Protocol, block.Cycle, (int)endorsingRight.Slots);

                    if (endorsingRight.Status == BakingRightStatus.Realized)
                    {
                        bakerCycle.Endorsements += (int)endorsingRight.Slots;
                        bakerCycle.EndorsementDeposits += GetEndorsementDeposit(block.Protocol, block.Cycle, (int)endorsingRight.Slots);
                    }
                    else if (endorsingRight.Status == BakingRightStatus.Uncovered)
                    {
                        bakerCycle.UncoveredEndorsements += (int)endorsingRight.Slots;
                    }
                    else if (endorsingRight.Status == BakingRightStatus.Missed)
                    {
                        bakerCycle.MissedEndorsements += (int)endorsingRight.Slots;
                    }
                    else
                    {
                        throw new Exception("Unexpected future rights");
                    }
                }
                #endregion

                #region endorsing rewards
                if (endorsingRight != null)
                {
                    bakerCycle.FutureEndorsementRewards -= GetFutureEndorsementReward(block.Protocol, block.Cycle, (int)endorsingRight.Slots);

                    var successReward = GetEndorsementReward(block.Protocol, block.Cycle, (int)endorsingRight.Slots, prevBlock.Priority);

                    var prevRights = prevBakingRights
                        .Where(x => x.Type == BakingRightType.Baking && x.BakerId == rights.Key)
                        .OrderBy(x => x.Priority)
                        .ToList();

                    var maxReward = prevRights.FirstOrDefault()?.Status > BakingRightStatus.Realized
                        ? GetEndorsementReward(block.Protocol, block.Cycle, (int)endorsingRight.Slots, (int)prevRights[0].Priority)
                        : successReward;

                    if (endorsingRight.Status == BakingRightStatus.Realized)
                        bakerCycle.EndorsementRewards += successReward;
                    else if (endorsingRight.Status == BakingRightStatus.Missed)
                        bakerCycle.MissedEndorsementRewards += successReward;
                    else if (endorsingRight.Status == BakingRightStatus.Uncovered)
                        bakerCycle.UncoveredEndorsementRewards += successReward;
                    else
                        throw new Exception("Unexpected future rights");

                    if (maxReward != successReward)
                    {
                        var prevBakerCycle = await Cache.BakerCycles.GetAsync(prevBlock.Cycle, rights.Key);
                        Db.TryAttach(prevBakerCycle);

                        if (prevRights[0].Status == BakingRightStatus.Missed)
                        {
                            if (prevRights[0].Priority == 0)
                                prevBakerCycle.MissedOwnBlockRewards += maxReward - successReward;
                            else
                                prevBakerCycle.MissedExtraBlockRewards += maxReward - successReward;
                        }
                        else
                        {
                            if (prevRights[0].Priority == 0)
                                prevBakerCycle.UncoveredOwnBlockRewards += maxReward - successReward;
                            else
                                prevBakerCycle.UncoveredExtraBlockRewards += maxReward - successReward;
                        }
                    }
                }
                #endregion

                #region baking rewards
                if (bakingRights.Count > 0)
                {
                    if (bakingRights[0].Priority == 0)
                        bakerCycle.FutureBlockRewards -= GetFutureBlockReward(block.Protocol, block.Cycle);

                    var successReward = GetBlockReward(block.Protocol, block.Cycle, (int)bakingRights[0].Priority, block.Validations);

                    var actualReward = bakingRights[^1].Status == BakingRightStatus.Realized
                        ? GetBlockReward(block.Protocol, block.Cycle, (int)bakingRights[^1].Priority, block.Validations)
                        : 0;

                    //var maxReward = endorsingRight?.Status > BakingRightStatus.Realized
                    //    ? GetBlockReward(block.Protocol, (int)bakingRights[0].Priority, block.Validations + (int)endorsingRight.Slots)
                    //    : successReward;

                    if (actualReward > 0)
                    {
                        if (bakingRights[^1].Priority == 0)
                            bakerCycle.OwnBlockRewards += actualReward;
                        else
                            bakerCycle.ExtraBlockRewards += actualReward;
                    }

                    if (successReward != actualReward)
                    {
                        if (bakingRights[0].Status == BakingRightStatus.Missed)
                        {
                            if (bakingRights[0].Priority == 0)
                                bakerCycle.MissedOwnBlockRewards += successReward - actualReward;
                            else
                                bakerCycle.MissedExtraBlockRewards += successReward - actualReward;
                        }
                        else
                        {
                            if (bakingRights[0].Priority == 0)
                                bakerCycle.UncoveredOwnBlockRewards += successReward - actualReward;
                            else
                                bakerCycle.UncoveredExtraBlockRewards += successReward - actualReward;
                        }
                    }

                    //if (maxReward != successReward)
                    //{
                    //    if (endorsingRight.Status == BakingRightStatus.Missed)
                    //        bakerCycle.MissedEndorsementRewards += maxReward - successReward;
                    //    else
                    //        bakerCycle.UncoveredEndorsementRewards += maxReward - successReward;
                    //}
                }
                #endregion

                #region fees
                if (bakingRights.Count > 0)
                {
                    if (bakingRights[^1].Status == BakingRightStatus.Realized)
                    {
                        if (bakingRights[^1].Priority == 0)
                            bakerCycle.OwnBlockFees += block.Fees;
                        else
                            bakerCycle.ExtraBlockFees += block.Fees;
                    }
                    else if (bakingRights[0].Status == BakingRightStatus.Missed)
                    {
                        if (bakingRights[0].Priority == 0)
                            bakerCycle.MissedOwnBlockFees += block.Fees;
                        else
                            bakerCycle.MissedExtraBlockFees += block.Fees;
                    }
                    else if (bakingRights[0].Status == BakingRightStatus.Uncovered)
                    {
                        if (bakingRights[0].Priority == 0)
                            bakerCycle.UncoveredOwnBlockFees += block.Fees;
                        else
                            bakerCycle.UncoveredExtraBlockFees += block.Fees;
                    }
                    else
                    {
                        throw new Exception("Unexpected future rights");
                    }
                }
                #endregion
            }

            if (block.DoubleBakings != null)
            {
                foreach (var op in block.DoubleBakings)
                {
                    var accusedBlock = await Cache.Blocks.GetAsync(op.AccusedLevel);
                    var offenderCycle = await Cache.BakerCycles.GetAsync(accusedBlock.Cycle, op.Offender.Id);
                    Db.TryAttach(offenderCycle);

                    offenderCycle.DoubleBakingLostDeposits += op.OffenderLostDeposit;
                    offenderCycle.DoubleBakingLostRewards += op.OffenderLostReward;
                    offenderCycle.DoubleBakingLostFees += op.OffenderLostFee;

                    var accuserCycle = await Cache.BakerCycles.GetAsync(block.Cycle, op.Accuser.Id);
                    Db.TryAttach(accuserCycle);

                    accuserCycle.DoubleBakingRewards += op.AccuserReward;
                }
            }

            if (block.DoubleEndorsings != null)
            {
                foreach (var op in block.DoubleEndorsings)
                {
                    var accusedBlock = await Cache.Blocks.GetAsync(op.AccusedLevel);
                    var offenderCycle = await Cache.BakerCycles.GetAsync(accusedBlock.Cycle, op.Offender.Id);
                    Db.TryAttach(offenderCycle);

                    offenderCycle.DoubleEndorsingLostDeposits += op.OffenderLostDeposit;
                    offenderCycle.DoubleEndorsingLostRewards += op.OffenderLostReward;
                    offenderCycle.DoubleEndorsingLostFees += op.OffenderLostFee;

                    var accuserCycle = await Cache.BakerCycles.GetAsync(block.Cycle, op.Accuser.Id);
                    Db.TryAttach(accuserCycle);

                    accuserCycle.DoubleEndorsingRewards += op.AccuserReward;
                }
            }

            if (block.Revelations != null)
            {
                foreach (var op in block.Revelations)
                {
                    var bakerCycle = await Cache.BakerCycles.GetAsync(block.Cycle, op.Baker.Id);
                    Db.TryAttach(bakerCycle);

                    bakerCycle.RevelationRewards += block.Protocol.RevelationReward;
                }
            }

            if (block.RevelationPenalties != null)
            {
                foreach (var op in block.RevelationPenalties)
                {
                    var penaltyBlock = await Cache.Blocks.GetAsync(op.MissedLevel);
                    var penaltyCycle = await Cache.BakerCycles.GetAsync(penaltyBlock.Cycle, op.Baker.Id);
                    Db.TryAttach(penaltyCycle);

                    penaltyCycle.RevelationLostRewards += op.LostReward;
                    penaltyCycle.RevelationLostFees += op.LostFees;
                }
            }
            #endregion

            #region new cycle
            if (block.Events.HasFlag(BlockEvents.CycleBegin))
            {
                //Only in Athens handler for better performance
                var snapshotBlock = await Cache.Blocks.GetAsync(futureCycle.SnapshotLevel);
                var snapshotProtocol = await Cache.Protocols.GetAsync(snapshotBlock.ProtoCode);
                //---------------------------------------------
                //TODO: add rolls to snapshot instead

                var bakerCycles = snapshots.Keys.ToDictionary(id => Cache.Accounts.GetDelegate(id).Address, id =>
                {
                    var snapshot = snapshots[id];

                    var rolls = (int)(snapshot.StakingBalance / snapshotProtocol.TokensPerRoll);
                    var rollsShare = (double)rolls / futureCycle.TotalRolls;

                    var bakerCycle = new BakerCycle
                    {
                        Cycle = futureCycle.Index,
                        BakerId = id,
                        Rolls = rolls,
                        StakingBalance = snapshot.StakingBalance,
                        DelegatedBalance = snapshot.DelegatedBalance,
                        DelegatorsCount = snapshot.DelegatorsCount,
                        ExpectedBlocks = block.Protocol.BlocksPerCycle * rollsShare,
                        ExpectedEndorsements = block.Protocol.EndorsersPerBlock * block.Protocol.BlocksPerCycle * rollsShare
                    };

                    return bakerCycle;
                });

                #region future baking rights
                foreach (var br in futureBakingRights)
                {
                    if (br.RequiredInt32("priority") > 0)
                        continue;

                    if (!bakerCycles.TryGetValue(br.RequiredString("delegate"), out var bakerCycle))
                        throw new Exception("Nonexistent baker cycle");

                    bakerCycle.FutureBlocks++;
                    bakerCycle.FutureBlockDeposits += GetBlockDeposit(block.Protocol, futureCycle.Index);
                    bakerCycle.FutureBlockRewards += GetFutureBlockReward(block.Protocol, futureCycle.Index);
                }
                #endregion

                #region future endorsing rights
                var skipLevel = futureEndorsingRights.Last().RequiredInt32("level");

                foreach (var er in futureEndorsingRights.TakeWhile(x => x.RequiredInt32("level") < skipLevel))
                {
                    if (!bakerCycles.TryGetValue(er.RequiredString("delegate"), out var bakerCycle))
                        throw new Exception("Nonexistent baker cycle");

                    var slots = er.RequiredArray("slots").Count();

                    bakerCycle.FutureEndorsements += slots;
                    bakerCycle.FutureEndorsementDeposits += GetEndorsementDeposit(block.Protocol, futureCycle.Index, slots);
                    bakerCycle.FutureEndorsementRewards += GetFutureEndorsementReward(block.Protocol, futureCycle.Index, slots);
                }
                #endregion

                #region shifted future endorsing rights
                // TODO: cache shifted rights
                var shiftedRights = await Db.BakingRights.AsNoTracking()
                    .Where(x => x.Level == futureCycle.FirstLevel && x.Type == BakingRightType.Endorsing)
                    .ToListAsync();

                foreach (var er in shiftedRights)
                {
                    var baker = Cache.Accounts.GetDelegate(er.BakerId);
                    if (!bakerCycles.TryGetValue(baker.Address, out var bakerCycle))
                    {
                        #region shifting hack
                        //shifting is actually a bad idea, but this is the lesser of two evils while Tezos protocol has bugs in the freezer.
                        var snapshotedBaker = await Proto.Rpc.GetDelegateAsync(futureCycle.SnapshotLevel, baker.Address);
                        var delegators = snapshotedBaker
                            .RequiredArray("delegated_contracts")
                            .EnumerateArray()
                            .Select(x => x.RequiredString())
                            .Where(x => x != baker.Address);

                        if (snapshotedBaker.RequiredInt32("grace_period") != block.Cycle - 3)
                            throw new Exception("Deactivated baker got baking rights");

                        var rolls = (int)(snapshotedBaker.RequiredInt64("staking_balance") / snapshotProtocol.TokensPerRoll);
                        var rollsShare = (double)rolls / futureCycle.TotalRolls;

                        bakerCycle = new BakerCycle
                        {
                            Cycle = futureCycle.Index,
                            BakerId = baker.Id,
                            Rolls = rolls,
                            StakingBalance = snapshotedBaker.RequiredInt64("staking_balance"),
                            DelegatedBalance = snapshotedBaker.RequiredInt64("delegated_balance"),
                            DelegatorsCount = delegators.Count(),
                            ExpectedBlocks = block.Protocol.BlocksPerCycle * rollsShare,
                            ExpectedEndorsements = block.Protocol.EndorsersPerBlock * block.Protocol.BlocksPerCycle * rollsShare
                        };
                        bakerCycles.Add(baker.Address, bakerCycle);

                        foreach (var delegatorAddress in delegators)
                        {
                            var snapshotedDelegator = await Proto.Rpc.GetContractAsync(futureCycle.SnapshotLevel, delegatorAddress);
                            Db.DelegatorCycles.Add(new DelegatorCycle
                            {
                                BakerId = baker.Id,
                                Balance = snapshotedDelegator.RequiredInt64("balance"),
                                Cycle = futureCycle.Index,
                                DelegatorId = (await Cache.Accounts.GetAsync(delegatorAddress)).Id
                            });
                        }
                        #endregion
                    }

                    bakerCycle.FutureEndorsements += (int)er.Slots;
                    bakerCycle.FutureEndorsementDeposits += GetEndorsementDeposit(block.Protocol, futureCycle.Index, (int)er.Slots);
                    bakerCycle.FutureEndorsementRewards += GetFutureEndorsementReward(block.Protocol, futureCycle.Index, (int)er.Slots);
                }
                #endregion

                Db.BakerCycles.AddRange(bakerCycles.Values);
            }
            #endregion
        }
    }
}
