using System.Text.Json;
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
            List<SnapshotBalance> snapshots,
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
                    .OrderBy(x => x.Round)
                    .ToList();

                var endorsingRight = rights
                    .FirstOrDefault(x => x.Type == BakingRightType.Endorsing);

                #region rights and deposits
                foreach (var br in bakingRights)
                {
                    if (br.Round == 0)
                    {
                        bakerCycle.FutureBlocks--;
                    }

                    if (br.Status == BakingRightStatus.Realized)
                    {
                        bakerCycle.Blocks++;
                    }
                    else if (br.Status == BakingRightStatus.Missed)
                    {
                        bakerCycle.MissedBlocks++;
                    }
                    else
                    {
                        throw new Exception("Unexpected future rights");
                    }
                }

                if (endorsingRight != null)
                {
                    bakerCycle.FutureEndorsements -= (int)endorsingRight.Slots;

                    if (endorsingRight.Status == BakingRightStatus.Realized)
                    {
                        bakerCycle.Endorsements += (int)endorsingRight.Slots;
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

                    var successReward = GetEndorsementReward(block.Protocol, block.Cycle, (int)endorsingRight.Slots, prevBlock.BlockRound);

                    var prevRights = prevBakingRights
                        .Where(x => x.Type == BakingRightType.Baking && x.BakerId == rights.Key)
                        .OrderBy(x => x.Round)
                        .ToList();

                    var maxReward = prevRights.FirstOrDefault()?.Status > BakingRightStatus.Realized
                        ? GetEndorsementReward(block.Protocol, block.Cycle, (int)endorsingRight.Slots, (int)prevRights[0].Round)
                        : successReward;

                    if (endorsingRight.Status == BakingRightStatus.Realized)
                        bakerCycle.EndorsementRewardsLiquid += successReward;
                    else if (endorsingRight.Status == BakingRightStatus.Missed)
                        bakerCycle.MissedEndorsementRewards += successReward;
                    else
                        throw new Exception("Unexpected future rights");

                    if (maxReward != successReward)
                    {
                        var prevBakerCycle = await Cache.BakerCycles.GetAsync(prevBlock.Cycle, rights.Key);
                        Db.TryAttach(prevBakerCycle);

                        prevBakerCycle.MissedBlockRewards += maxReward - successReward;
                    }
                }
                #endregion

                #region baking rewards
                if (bakingRights.Count > 0)
                {
                    if (bakingRights[0].Round == 0)
                        bakerCycle.FutureBlockRewards -= GetFutureBlockReward(block.Protocol, block.Cycle);

                    var successReward = GetBlockReward(block.Protocol, block.Cycle, (int)bakingRights[0].Round, block.Validations);

                    var actualReward = bakingRights[^1].Status == BakingRightStatus.Realized
                        ? GetBlockReward(block.Protocol, block.Cycle, (int)bakingRights[^1].Round, block.Validations)
                        : 0;

                    //var maxReward = endorsingRight?.Status > BakingRightStatus.Realized
                    //    ? GetBlockReward(block.Protocol, (int)bakingRights[0].Round, block.Validations + (int)endorsingRight.Slots)
                    //    : successReward;

                    if (actualReward > 0)
                    {
                        bakerCycle.BlockRewardsLiquid += actualReward;
                    }

                    if (successReward != actualReward)
                    {
                        bakerCycle.MissedBlockRewards += successReward - actualReward;
                    }

                    //if (maxReward != successReward)
                    //{
                    //    bakerCycle.MissedEndorsementRewards += maxReward - successReward;
                    //}
                }
                #endregion

                #region fees
                if (bakingRights.Count > 0)
                {
                    if (bakingRights[^1].Status == BakingRightStatus.Realized)
                    {
                        bakerCycle.BlockFees += block.Fees;
                    }
                    else if (bakingRights[0].Status == BakingRightStatus.Missed)
                    {
                        bakerCycle.MissedBlockFees += block.Fees;
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

                    offenderCycle.DoubleBakingLossesOwn += op.OffenderLossOwn;

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

                    offenderCycle.DoubleEndorsingLossesOwn += op.OffenderLossOwn;

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

                    bakerCycle.NonceRevelationRewardsLiquid += op.RewardLiquid;
                }
            }

            if (block.RevelationPenalties != null)
            {
                foreach (var op in block.RevelationPenalties)
                {
                    var penaltyBlock = await Cache.Blocks.GetAsync(op.MissedLevel);
                    var penaltyCycle = await Cache.BakerCycles.GetAsync(penaltyBlock.Cycle, op.Baker.Id);
                    Db.TryAttach(penaltyCycle);

                    penaltyCycle.NonceRevelationLosses += op.Loss;
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

                var bakerCycles = snapshots.ToDictionary(
                    snapshot => Cache.Accounts.GetDelegate(snapshot.AccountId).Address,
                    snapshot =>
                    {
                        var bakingPower = snapshot.StakingBalance - snapshot.StakingBalance % snapshotProtocol.MinimalStake;
                        var share = (double)bakingPower / futureCycle.TotalBakingPower;

                        var bakerCycle = new BakerCycle
                        {
                            Cycle = futureCycle.Index,
                            BakerId = snapshot.AccountId,
                            OwnDelegatedBalance = snapshot.OwnDelegatedBalance,
                            ExternalDelegatedBalance = snapshot.ExternalDelegatedBalance,
                            DelegatorsCount = snapshot.DelegatorsCount,
                            OwnStakedBalance = snapshot.OwnStakedBalance,
                            ExternalStakedBalance = snapshot.ExternalStakedBalance,
                            StakersCount = snapshot.StakersCount,
                            BakingPower = bakingPower,
                            TotalBakingPower = futureCycle.TotalBakingPower,
                            ExpectedBlocks = block.Protocol.BlocksPerCycle * share,
                            ExpectedEndorsements = block.Protocol.EndorsersPerBlock * block.Protocol.BlocksPerCycle * share
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
                        var snapshottedBaker = await Proto.Rpc.GetDelegateAsync(futureCycle.SnapshotLevel, baker.Address);
                        var delegators = snapshottedBaker
                            .RequiredArray("delegated_contracts")
                            .EnumerateArray()
                            .Select(x => x.RequiredString())
                            .Where(x => x != baker.Address);

                        if (snapshottedBaker.RequiredInt32("grace_period") != block.Cycle - 3)
                            throw new Exception("Deactivated baker got baking rights");

                        var stakingBalance = snapshottedBaker.RequiredInt64("staking_balance");
                        var delegatedBalance = snapshottedBaker.RequiredInt64("delegated_balance");
                        var bakingPower = stakingBalance - stakingBalance % snapshotProtocol.MinimalStake;
                        var share = (double)bakingPower / futureCycle.TotalBakingPower;

                        bakerCycle = new BakerCycle
                        {
                            Cycle = futureCycle.Index,
                            BakerId = baker.Id,
                            OwnDelegatedBalance = stakingBalance - delegatedBalance,
                            ExternalDelegatedBalance = delegatedBalance,
                            DelegatorsCount = delegators.Count(),
                            OwnStakedBalance = 0,
                            ExternalStakedBalance = 0,
                            StakersCount = 0,
                            BakingPower = bakingPower,
                            TotalBakingPower = futureCycle.TotalBakingPower,
                            ExpectedBlocks = block.Protocol.BlocksPerCycle * share,
                            ExpectedEndorsements = block.Protocol.EndorsersPerBlock * block.Protocol.BlocksPerCycle * share
                        };
                        bakerCycles.Add(baker.Address, bakerCycle);

                        foreach (var delegatorAddress in delegators)
                        {
                            var snapshottedDelegator = await Proto.Rpc.GetContractAsync(futureCycle.SnapshotLevel, delegatorAddress);
                            Db.DelegatorCycles.Add(new DelegatorCycle
                            {
                                Cycle = futureCycle.Index,
                                DelegatorId = (await Cache.Accounts.GetAsync(delegatorAddress)).Id,
                                BakerId = baker.Id,
                                DelegatedBalance = snapshottedDelegator.RequiredInt64("balance"),
                                StakedBalance = 0
                            });
                        }
                        #endregion
                    }

                    bakerCycle.FutureEndorsements += (int)er.Slots;
                    bakerCycle.FutureEndorsementRewards += GetFutureEndorsementReward(block.Protocol, futureCycle.Index, (int)er.Slots);
                }
                #endregion

                Db.BakerCycles.AddRange(bakerCycles.Values);
            }
            #endregion
        }
    }
}
