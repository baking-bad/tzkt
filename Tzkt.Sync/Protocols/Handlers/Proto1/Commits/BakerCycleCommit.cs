using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto1
{
    class BakerCycleCommit : ProtocolCommit
    {
        public BakerCycleCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual async Task Apply(
            Block block,
            Cycle futureCycle,
            IEnumerable<JsonElement> futureBakingRights,
            IEnumerable<JsonElement> futureEndorsingRights,
            Dictionary<int, CycleCommit.DelegateSnapshot> snapshots,
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
                    if (br.Round == 0 && bakerCycle.FutureBlocks != 0) // FutureBlocks is always 0 for weirds
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
                    if (bakerCycle.FutureEndorsements != 0) // FutureEndorsements is always 0 for weirds
                    {
                        bakerCycle.FutureEndorsements -= (int)endorsingRight.Slots;
                    }

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
                    if (bakerCycle.FutureEndorsementRewards != 0) // FutureEndorsementRewards is always 0 for weirds
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
                        bakerCycle.EndorsementRewards += successReward;
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
                    if (bakingRights[0].Round == 0 && bakerCycle.FutureBlockRewards != 0) // FutureBlockRewards is always 0 for weirds
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
                        bakerCycle.BlockRewards += actualReward;
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

                    offenderCycle.DoubleBakingLosses += op.OffenderLoss;

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

                    offenderCycle.DoubleEndorsingLosses += op.OffenderLoss;

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

                    bakerCycle.RevelationRewards += op.Reward;
                }
            }

            if (block.RevelationPenalties != null)
            {
                foreach (var op in block.RevelationPenalties)
                {
                    var penaltyBlock = await Cache.Blocks.GetAsync(op.MissedLevel);
                    var penaltyCycle = await Cache.BakerCycles.GetAsync(penaltyBlock.Cycle, op.Baker.Id);
                    Db.TryAttach(penaltyCycle);

                    penaltyCycle.RevelationLosses += op.Loss;
                }
            }
            #endregion

            #region new cycle
            if (block.Events.HasFlag(BlockEvents.CycleBegin))
            {
                var bakerCycles = new Dictionary<string, BakerCycle>(snapshots.Count);
                foreach (var kv in snapshots)
                {
                    var baker = await Cache.Accounts.GetAsync(kv.Key); // WTF: rights were given to non-baker accounts
                    var snapshot = snapshots[kv.Key];

                    var activeStake = snapshot.StakingBalance - snapshot.StakingBalance % block.Protocol.TokensPerRoll;
                    var share = (double)activeStake / futureCycle.SelectedStake;

                    var bakerCycle = new BakerCycle
                    {
                        Cycle = futureCycle.Index,
                        BakerId = kv.Key,
                        StakingBalance = snapshot.StakingBalance,
                        ActiveStake = activeStake,
                        SelectedStake = futureCycle.SelectedStake,
                        DelegatedBalance = snapshot.DelegatedBalance,
                        DelegatorsCount = snapshot.DelegatorsCount,
                        ExpectedBlocks = block.Protocol.BlocksPerCycle * share,
                        ExpectedEndorsements = block.Protocol.EndorsersPerBlock * block.Protocol.BlocksPerCycle * share
                    };

                    bakerCycles.Add(baker.Address, bakerCycle);
                }

                #region future baking rights
                foreach (var br in futureBakingRights)
                {
                    if (br.RequiredInt32("priority") > 0)
                        continue;

                    if (!bakerCycles.TryGetValue(br.RequiredString("delegate"), out var bakerCycle))
                        throw new Exception("Nonexistent baker cycle");

                    if (!Cache.Accounts.DelegateExists(br.RequiredString("delegate")))
                        continue;

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

                    if (!Cache.Accounts.DelegateExists(er.RequiredString("delegate")))
                        continue;

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
                    if (!Cache.Accounts.DelegateExists(er.BakerId))
                        continue;

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
                        var activeStake = stakingBalance - stakingBalance % block.Protocol.TokensPerRoll;
                        var share = (double)activeStake / futureCycle.SelectedStake;

                        bakerCycle = new BakerCycle
                        {
                            Cycle = futureCycle.Index,
                            BakerId = baker.Id,
                            StakingBalance = stakingBalance,
                            ActiveStake = activeStake,
                            SelectedStake = futureCycle.SelectedStake,
                            DelegatedBalance = snapshottedBaker.RequiredInt64("delegated_balance"),
                            DelegatorsCount = delegators.Count(),
                            ExpectedBlocks = block.Protocol.BlocksPerCycle * share,
                            ExpectedEndorsements = block.Protocol.EndorsersPerBlock * block.Protocol.BlocksPerCycle * share
                        };
                        bakerCycles.Add(baker.Address, bakerCycle);

                        foreach (var delegatorAddress in delegators)
                        {
                            var snapshottedDelegator = await Proto.Rpc.GetContractAsync(futureCycle.SnapshotLevel, delegatorAddress);
                            Db.DelegatorCycles.Add(new DelegatorCycle
                            {
                                BakerId = baker.Id,
                                Balance = snapshottedDelegator.RequiredInt64("balance"),
                                Cycle = futureCycle.Index,
                                DelegatorId = (await Cache.Accounts.GetAsync(delegatorAddress)).Id
                            });
                        }
                        #endregion
                    }

                    bakerCycle.FutureEndorsements += (int)er.Slots;
                    bakerCycle.FutureEndorsementRewards += GetFutureEndorsementReward(block.Protocol, futureCycle.Index, (int)er.Slots);
                }
                #endregion

                Db.BakerCycles.AddRange(bakerCycles.Values);

                #region weird bakers
                if (block.Cycle > 0)
                {
                    //one-way change...
                    await Db.Database.ExecuteSqlRawAsync($@"
                        DELETE FROM ""BakerCycles"" as bc
                        USING ""Accounts"" as acc
                        WHERE acc.""Id"" = bc.""BakerId""
                        AND bc.""Cycle"" = {block.Cycle - 1}
                        AND acc.""Type"" != {(int)AccountType.Delegate}");
                }
                #endregion
            }
            #endregion
        }

        public virtual async Task Revert(Block block)
        {
            block.Protocol ??= await Cache.Protocols.GetAsync(block.ProtoCode);

            #region current rights
            var prevBlock = await Cache.Blocks.PreviousAsync();
            var prevBakingRights = prevBlock.Level == 1 ? new List<BakingRight>(0)
                : await Cache.BakingRights.GetAsync(prevBlock.Cycle, prevBlock.Level);

            var currentRights = await Cache.BakingRights.GetAsync(block.Cycle, block.Level);

            foreach (var rights in currentRights.GroupBy(x => x.BakerId))
            {
                var bakerCycle = await Cache.BakerCycles.GetOrDefaultAsync(block.Cycle, rights.Key);
                if (bakerCycle == null)
                {
                    if (!block.Events.HasFlag(BlockEvents.CycleBegin) || rights.Any(x => x.Status == BakingRightStatus.Realized))
                        throw new Exception("Shifted rights hack doesn't work :(");
                    continue;
                }

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
                        bakerCycle.FutureBlocks++;
                    }

                    if (br.Status == BakingRightStatus.Realized)
                    {
                        bakerCycle.Blocks--;
                    }
                    else if (br.Status == BakingRightStatus.Missed)
                    {
                        bakerCycle.MissedBlocks--;
                    }
                    else
                    {
                        throw new Exception("Unexpected future rights");
                    }
                }

                if (endorsingRight != null)
                {
                    bakerCycle.FutureEndorsements += (int)endorsingRight.Slots;

                    if (endorsingRight.Status == BakingRightStatus.Realized)
                    {
                        bakerCycle.Endorsements -= (int)endorsingRight.Slots;
                    }
                    else if (endorsingRight.Status == BakingRightStatus.Missed)
                    {
                        bakerCycle.MissedEndorsements -= (int)endorsingRight.Slots;
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
                    bakerCycle.FutureEndorsementRewards += GetFutureEndorsementReward(block.Protocol, block.Cycle, (int)endorsingRight.Slots);

                    var successReward = GetEndorsementReward(block.Protocol, block.Cycle, (int)endorsingRight.Slots, prevBlock.BlockRound);

                    var prevRights = prevBakingRights
                        .Where(x => x.Type == BakingRightType.Baking && x.BakerId == rights.Key)
                        .OrderBy(x => x.Round)
                        .ToList();

                    var maxReward = prevRights.FirstOrDefault()?.Status > BakingRightStatus.Realized
                        ? GetEndorsementReward(block.Protocol, block.Cycle, (int)endorsingRight.Slots, (int)prevRights[0].Round)
                        : successReward;

                    if (endorsingRight.Status == BakingRightStatus.Realized)
                        bakerCycle.EndorsementRewards -= successReward;
                    else if (endorsingRight.Status == BakingRightStatus.Missed)
                        bakerCycle.MissedEndorsementRewards -= successReward;
                    else
                        throw new Exception("Unexpected future rights");

                    if (maxReward != successReward)
                    {
                        var prevBakerCycle = await Cache.BakerCycles.GetAsync(prevBlock.Cycle, rights.Key);
                        Db.TryAttach(prevBakerCycle);

                        prevBakerCycle.MissedBlockRewards -= maxReward - successReward;
                    }
                }
                #endregion

                #region baking rewards
                if (bakingRights.Count > 0)
                {
                    if (bakingRights[0].Round == 0)
                        bakerCycle.FutureBlockRewards += GetFutureBlockReward(block.Protocol, block.Cycle);

                    var successReward = GetBlockReward(block.Protocol, block.Cycle, (int)bakingRights[0].Round, block.Validations);

                    var actualReward = bakingRights[^1].Status == BakingRightStatus.Realized
                        ? GetBlockReward(block.Protocol, block.Cycle, (int)bakingRights[^1].Round, block.Validations)
                        : 0;

                    //var maxReward = endorsingRight?.Status > BakingRightStatus.Realized
                    //    ? GetBlockReward(block.Protocol, (int)bakingRights[0].Round, block.Validations + (int)endorsingRight.Slots)
                    //    : successReward;

                    if (actualReward > 0)
                    {
                        bakerCycle.BlockRewards -= actualReward;
                    }

                    if (successReward != actualReward)
                    {
                        bakerCycle.MissedBlockRewards -= successReward - actualReward;
                    }

                    //if (maxReward != successReward)
                    //{
                    //    bakerCycle.MissedEndorsementRewards -= maxReward - successReward;
                    //}
                }
                #endregion

                #region fees
                if (bakingRights.Count > 0)
                {
                    if (bakingRights[^1].Status == BakingRightStatus.Realized)
                    {
                        bakerCycle.BlockFees -= block.Fees;
                    }
                    else if (bakingRights[0].Status == BakingRightStatus.Missed)
                    {
                        bakerCycle.MissedBlockFees -= block.Fees;
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
                    var offenderCycle = await Cache.BakerCycles.GetAsync(accusedBlock.Cycle, op.OffenderId);
                    Db.TryAttach(offenderCycle);

                    offenderCycle.DoubleBakingLosses -= op.OffenderLoss;

                    var accuserCycle = await Cache.BakerCycles.GetAsync(block.Cycle, op.AccuserId);
                    Db.TryAttach(accuserCycle);

                    accuserCycle.DoubleBakingRewards -= op.AccuserReward;
                }
            }

            if (block.DoubleEndorsings != null)
            {
                foreach (var op in block.DoubleEndorsings)
                {
                    var accusedBlock = await Cache.Blocks.GetAsync(op.AccusedLevel);
                    var offenderCycle = await Cache.BakerCycles.GetAsync(accusedBlock.Cycle, op.OffenderId);
                    Db.TryAttach(offenderCycle);

                    offenderCycle.DoubleEndorsingLosses -= op.OffenderLoss;

                    var accuserCycle = await Cache.BakerCycles.GetAsync(block.Cycle, op.AccuserId);
                    Db.TryAttach(accuserCycle);

                    accuserCycle.DoubleEndorsingRewards -= op.AccuserReward;
                }
            }

            if (block.Revelations != null)
            {
                foreach (var op in block.Revelations)
                {
                    var bakerCycle = await Cache.BakerCycles.GetAsync(block.Cycle, op.BakerId);
                    Db.TryAttach(bakerCycle);

                    bakerCycle.RevelationRewards -= op.Reward;
                }
            }

            if (block.RevelationPenalties != null)
            {
                foreach (var op in block.RevelationPenalties)
                {
                    var penaltyBlock = await Cache.Blocks.GetAsync(op.MissedLevel);
                    var penaltyCycle = await Cache.BakerCycles.GetAsync(penaltyBlock.Cycle, op.BakerId);
                    Db.TryAttach(penaltyCycle);

                    penaltyCycle.RevelationLosses -= op.Loss;
                }
            }
            #endregion

            #region new cycle
            if (block.Events.HasFlag(BlockEvents.CycleBegin))
            {
                await Db.Database.ExecuteSqlRawAsync($@"
                    DELETE  FROM ""BakerCycles""
                    WHERE   ""Cycle"" = {block.Cycle + block.Protocol.PreservedCycles}");
            }
            #endregion
        }

        #region helpers
        protected long GetFutureBlockReward(Protocol protocol, int cycle)
            => cycle < protocol.NoRewardCycles ? 0 : protocol.BlockReward0;

        protected long GetFutureEndorsementReward(Protocol protocol, int cycle, int slots)
            => cycle < protocol.NoRewardCycles ? 0 : (slots * protocol.EndorsementReward0);

        protected long GetBlockReward(Protocol protocol, int cycle, int priority, int slots)
            => cycle < protocol.NoRewardCycles ? 0 : protocol.BlockReward0;

        protected long GetEndorsementReward(Protocol protocol, int cycle, int slots, int prevPriority)
            => cycle < protocol.NoRewardCycles ? 0 : (slots * (long)(protocol.EndorsementReward0 / (prevPriority + 1.0)));
        #endregion
    }
}
