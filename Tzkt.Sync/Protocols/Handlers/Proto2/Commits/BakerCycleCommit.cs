using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto2
{
    class BakerCycleCommit : ProtocolCommit
    {
        public Block Block { get; private set; }
        public Cycle FutureCycle { get; private set; }
        public List<RawBakingRight> FutureBakingRights { get; private set; }
        public List<RawEndorsingRight> FutureEndorsingRights { get; private set; }
        public Dictionary<int, CycleCommit.DelegateSnapshot> Snapshots { get; private set; }
        public List<BakingRight> CurrentRights { get; private set; }

        BakerCycleCommit(ProtocolHandler protocol) : base(protocol) { }

        public override async Task Apply()
        {
            var cycle = (Block.Level - 1) / Block.Protocol.BlocksPerCycle;

            #region current rights
            var prevBlock = await Cache.Blocks.CurrentAsync();
            var prevBakingRights = prevBlock.Level == 1 ? new List<BakingRight>(0)
                : await Cache.BakingRights.GetAsync((prevBlock.Level - 1) / Block.Protocol.BlocksPerCycle, prevBlock.Level);

            foreach (var rights in CurrentRights.GroupBy(x => x.BakerId))
            {
                var bakerCycle = await Cache.BakerCycles.GetAsync(cycle, rights.Key);
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
                    if (br.Priority == 0 && bakerCycle.FutureBlocks != 0) // FutureBlocks is always 0 for weirds
                    {
                        bakerCycle.FutureBlocks--;
                        bakerCycle.FutureBlockDeposits -= GetBlockDeposit(Block.Protocol, cycle);
                    }

                    if (br.Status == BakingRightStatus.Realized)
                    {
                        bakerCycle.BlockDeposits += GetBlockDeposit(Block.Protocol, cycle);

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
                    if (bakerCycle.FutureEndorsements != 0) // FutureEndorsements is always 0 for weirds
                    {
                        bakerCycle.FutureEndorsements -= (int)endorsingRight.Slots;
                        bakerCycle.FutureEndorsementDeposits -= GetEndorsementDeposit(Block.Protocol, cycle, (int)endorsingRight.Slots);
                    }

                    if (endorsingRight.Status == BakingRightStatus.Realized)
                    {
                        bakerCycle.Endorsements += (int)endorsingRight.Slots;
                        bakerCycle.EndorsementDeposits += GetEndorsementDeposit(Block.Protocol, cycle, (int)endorsingRight.Slots);
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
                    if (bakerCycle.FutureEndorsementRewards != 0) // FutureEndorsementRewards is always 0 for weirds
                        bakerCycle.FutureEndorsementRewards -= GetFutureEndorsementReward(Block.Protocol, cycle, (int)endorsingRight.Slots);

                    var successReward = GetEndorsementReward(Block.Protocol, (int)endorsingRight.Slots, prevBlock.Priority);

                    var prevRights = prevBakingRights
                        .Where(x => x.Type == BakingRightType.Baking && x.BakerId == rights.Key)
                        .OrderBy(x => x.Priority)
                        .ToList();

                    var maxReward = prevRights.FirstOrDefault()?.Status > BakingRightStatus.Realized
                        ? GetEndorsementReward(Block.Protocol, (int)endorsingRight.Slots, (int)prevRights[0].Priority)
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
                        var prevBakerCycle = await Cache.BakerCycles.GetAsync((prevBlock.Level - 1) / Block.Protocol.BlocksPerCycle, rights.Key);
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
                    if (bakingRights[0].Priority == 0 && bakerCycle.FutureBlockRewards != 0) // FutureBlockRewards is always 0 for weirds
                        bakerCycle.FutureBlockRewards -= GetFutureBlockReward(Block.Protocol, cycle);

                    var successReward = GetBlockReward(Block.Protocol, (int)bakingRights[0].Priority, Block.Validations);

                    var actualReward = bakingRights[^1].Status == BakingRightStatus.Realized
                        ? GetBlockReward(Block.Protocol, (int)bakingRights[^1].Priority, Block.Validations)
                        : 0;

                    //var maxReward = endorsingRight?.Status > BakingRightStatus.Realized
                    //    ? GetBlockReward(Block.Protocol, (int)bakingRights[0].Priority, Block.Validations + (int)endorsingRight.Slots)
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
                            bakerCycle.OwnBlockFees += Block.Fees;
                        else
                            bakerCycle.ExtraBlockFees += Block.Fees;
                    }
                    else if (bakingRights[0].Status == BakingRightStatus.Missed)
                    {
                        if (bakingRights[0].Priority == 0)
                            bakerCycle.MissedOwnBlockFees += Block.Fees;
                        else
                            bakerCycle.MissedExtraBlockFees += Block.Fees;
                    }
                    else if (bakingRights[0].Status == BakingRightStatus.Uncovered)
                    {
                        if (bakingRights[0].Priority == 0)
                            bakerCycle.UncoveredOwnBlockFees += Block.Fees;
                        else
                            bakerCycle.UncoveredExtraBlockFees += Block.Fees;
                    }
                    else
                    {
                        throw new Exception("Unexpected future rights");
                    }
                }
                #endregion
            }

            if (Block.DoubleBakings != null)
            {
                foreach (var op in Block.DoubleBakings)
                {
                    var offenderCycle = await Cache.BakerCycles.GetAsync((op.AccusedLevel - 1) / Block.Protocol.BlocksPerCycle, op.Offender.Id);
                    Db.TryAttach(offenderCycle);

                    offenderCycle.AccusationLostDeposits += op.OffenderLostDeposit;
                    offenderCycle.AccusationLostRewards += op.OffenderLostReward;
                    offenderCycle.AccusationLostFees += op.OffenderLostFee;

                    var accuserCycle = await Cache.BakerCycles.GetAsync(cycle, op.Accuser.Id);
                    Db.TryAttach(accuserCycle);

                    accuserCycle.AccusationRewards += op.AccuserReward;
                }
            }

            if (Block.DoubleEndorsings != null)
            {
                foreach (var op in Block.DoubleEndorsings)
                {
                    var offenderCycle = await Cache.BakerCycles.GetAsync((op.AccusedLevel - 1) / Block.Protocol.BlocksPerCycle, op.Offender.Id);
                    Db.TryAttach(offenderCycle);

                    offenderCycle.AccusationLostDeposits += op.OffenderLostDeposit;
                    offenderCycle.AccusationLostRewards += op.OffenderLostReward;
                    offenderCycle.AccusationLostFees += op.OffenderLostFee;

                    var accuserCycle = await Cache.BakerCycles.GetAsync(cycle, op.Accuser.Id);
                    Db.TryAttach(accuserCycle);

                    accuserCycle.AccusationRewards += op.AccuserReward;
                }
            }

            if (Block.Revelations != null)
            {
                foreach (var op in Block.Revelations)
                {
                    var bakerCycle = await Cache.BakerCycles.GetAsync(cycle, op.Baker.Id);
                    Db.TryAttach(bakerCycle);

                    bakerCycle.RevelationRewards += Block.Protocol.RevelationReward;
                }
            }

            if (Block.RevelationPenalties != null)
            {
                foreach (var op in Block.RevelationPenalties)
                {
                    var penaltyCycle = await Cache.BakerCycles.GetAsync((op.MissedLevel - 1) / Block.Protocol.BlocksPerCycle, op.Baker.Id);
                    Db.TryAttach(penaltyCycle);

                    penaltyCycle.RevelationLostRewards += op.LostReward;
                    penaltyCycle.RevelationLostFees += op.LostFees;
                }
            }
            #endregion

            #region new cycle
            if (Block.Events.HasFlag(BlockEvents.CycleBegin))
            {
                var bakerCycles = new Dictionary<string, BakerCycle>(Snapshots.Count);
                foreach (var kv in Snapshots)
                {
                    var baker = await Cache.Accounts.GetAsync(kv.Key); // WTF: rights were given to non-baker accounts
                    var snapshot = Snapshots[kv.Key];

                    var rolls = (int)(snapshot.StakingBalance / Block.Protocol.TokensPerRoll);
                    var rollsShare = (double)rolls / FutureCycle.TotalRolls;

                    var bakerCycle = new BakerCycle
                    {
                        Cycle = FutureCycle.Index,
                        BakerId = kv.Key,
                        Rolls = rolls,
                        StakingBalance = snapshot.StakingBalance,
                        DelegatedBalance = snapshot.DelegatedBalance,
                        DelegatorsCount = snapshot.DelegatorsCount,
                        ExpectedBlocks = Block.Protocol.BlocksPerCycle * rollsShare,
                        ExpectedEndorsements = Block.Protocol.EndorsersPerBlock * Block.Protocol.BlocksPerCycle * rollsShare
                    };

                    bakerCycles.Add(baker.Address, bakerCycle);
                }

                #region future baking rights
                foreach (var br in FutureBakingRights)
                {
                    if (br.Priority > 0)
                        continue;

                    if (!bakerCycles.TryGetValue(br.Delegate, out var bakerCycle))
                        throw new Exception("Nonexistent baker cycle");

                    if (!Cache.Accounts.DelegateExists(br.Delegate))
                        continue;

                    bakerCycle.FutureBlocks++;
                    bakerCycle.FutureBlockDeposits += GetBlockDeposit(Block.Protocol, FutureCycle.Index);
                    bakerCycle.FutureBlockRewards += GetFutureBlockReward(Block.Protocol, FutureCycle.Index);
                }
                #endregion

                #region future endorsing rights
                var skipLevel = FutureEndorsingRights[^1].Level;

                foreach (var er in FutureEndorsingRights.TakeWhile(x => x.Level < skipLevel))
                {
                    if (!bakerCycles.TryGetValue(er.Delegate, out var bakerCycle))
                        throw new Exception("Nonexistent baker cycle");

                    if (!Cache.Accounts.DelegateExists(er.Delegate))
                        continue;

                    bakerCycle.FutureEndorsements += er.Slots.Count;
                    bakerCycle.FutureEndorsementDeposits += GetEndorsementDeposit(Block.Protocol, FutureCycle.Index, er.Slots.Count);
                    bakerCycle.FutureEndorsementRewards += GetFutureEndorsementReward(Block.Protocol, FutureCycle.Index, er.Slots.Count);
                }
                #endregion

                #region shifted future endorsing rights
                // TODO: cache shifted rights
                var shiftedLevel = FutureCycle.Index * Block.Protocol.BlocksPerCycle + 1;
                var shiftedRights = await Db.BakingRights.AsNoTracking()
                    .Where(x => x.Level == shiftedLevel && x.Type == BakingRightType.Endorsing)
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
                        var snapshotedBaker = await (Proto.Diagnostics as Diagnostics).GetRemoteDelegate(FutureCycle.SnapshotLevel, baker.Address);
                        var delegators = snapshotedBaker.Delegators.Select(x => ((System.Text.Json.JsonElement)x).GetString()).Where(x => x != baker.Address);

                        if (snapshotedBaker.GracePeriod != (FutureCycle.SnapshotLevel - 1) / Block.Protocol.BlocksPerCycle - 1)
                            throw new Exception("Deactivated baker got baking rights");

                        var rolls = (int)(snapshotedBaker.StakingBalance / Block.Protocol.TokensPerRoll);
                        var rollsShare = (double)rolls / FutureCycle.TotalRolls;

                        bakerCycle = new BakerCycle
                        {
                            Cycle = FutureCycle.Index,
                            BakerId = baker.Id,
                            Rolls = rolls,
                            StakingBalance = snapshotedBaker.StakingBalance,
                            DelegatedBalance = snapshotedBaker.DelegatedBalance,
                            DelegatorsCount = delegators.Count(),
                            ExpectedBlocks = Block.Protocol.BlocksPerCycle * rollsShare,
                            ExpectedEndorsements = Block.Protocol.EndorsersPerBlock * Block.Protocol.BlocksPerCycle * rollsShare
                        };
                        bakerCycles.Add(baker.Address, bakerCycle);

                        foreach (var delegatorAddress in delegators)
                        {
                            var snapshotedDelegator = await (Proto.Diagnostics as Diagnostics).GetRemoteContract(FutureCycle.SnapshotLevel, delegatorAddress);
                            Db.DelegatorCycles.Add(new DelegatorCycle
                            {
                                BakerId = baker.Id,
                                Balance = snapshotedDelegator.Balance,
                                Cycle = FutureCycle.Index,
                                DelegatorId = (await Cache.Accounts.GetAsync(delegatorAddress)).Id
                            });
                        }
                        #endregion
                    }

                    bakerCycle.FutureEndorsements += (int)er.Slots;
                    bakerCycle.FutureEndorsementDeposits += GetEndorsementDeposit(Block.Protocol, FutureCycle.Index, (int)er.Slots);
                    bakerCycle.FutureEndorsementRewards += GetFutureEndorsementReward(Block.Protocol, FutureCycle.Index, (int)er.Slots);
                }
                #endregion

                Db.BakerCycles.AddRange(bakerCycles.Values);

                #region weird bakers
                if (cycle > 0)
                {
                    //one-way change...
                    await Db.Database.ExecuteSqlRawAsync($@"
                        DELETE FROM ""BakerCycles"" as bc
                        USING ""Accounts"" as acc
                        WHERE acc.""Id"" = bc.""BakerId""
                        AND bc.""Cycle"" = {cycle - 1}
                        AND acc.""Type"" != {(int)AccountType.Delegate}");
                }
                #endregion
            }
            #endregion
        }

        public override async Task Revert()
        {
            Block.Protocol ??= await Cache.Protocols.GetAsync(Block.ProtoCode);
            var cycle = (Block.Level - 1) / Block.Protocol.BlocksPerCycle;

            #region current rights
            var prevBlock = await Cache.Blocks.PreviousAsync();
            var prevBakingRights = prevBlock.Level == 1 ? new List<BakingRight>(0)
                : await Cache.BakingRights.GetAsync((prevBlock.Level - 1) / Block.Protocol.BlocksPerCycle, prevBlock.Level);

            CurrentRights = await Cache.BakingRights.GetAsync(cycle, Block.Level);

            foreach (var rights in CurrentRights.GroupBy(x => x.BakerId))
            {
                var bakerCycle = await Cache.BakerCycles.GetOrDefaultAsync(cycle, rights.Key);
                if (bakerCycle == null)
                {
                    if (!Block.Events.HasFlag(BlockEvents.CycleBegin) || rights.Any(x => x.Status == BakingRightStatus.Realized))
                        throw new Exception("Shifted rights hack doesn't work :(");
                    continue;
                }

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
                        bakerCycle.FutureBlocks++;
                        bakerCycle.FutureBlockDeposits += GetBlockDeposit(Block.Protocol, cycle);
                    }

                    if (br.Status == BakingRightStatus.Realized)
                    {
                        bakerCycle.BlockDeposits -= GetBlockDeposit(Block.Protocol, cycle);

                        if (br.Priority == 0)
                            bakerCycle.OwnBlocks--;
                        else
                            bakerCycle.ExtraBlocks--;
                    }
                    else if (br.Status == BakingRightStatus.Uncovered)
                    {
                        if (br.Priority == 0)
                            bakerCycle.UncoveredOwnBlocks--;
                        else
                            bakerCycle.UncoveredExtraBlocks--;
                    }
                    else if (br.Status == BakingRightStatus.Missed)
                    {
                        if (br.Priority == 0)
                            bakerCycle.MissedOwnBlocks--;
                        else
                            bakerCycle.MissedExtraBlocks--;
                    }
                    else
                    {
                        throw new Exception("Unexpected future rights");
                    }
                }

                if (endorsingRight != null)
                {
                    bakerCycle.FutureEndorsements += (int)endorsingRight.Slots;
                    bakerCycle.FutureEndorsementDeposits += GetEndorsementDeposit(Block.Protocol, cycle, (int)endorsingRight.Slots);

                    if (endorsingRight.Status == BakingRightStatus.Realized)
                    {
                        bakerCycle.Endorsements -= (int)endorsingRight.Slots;
                        bakerCycle.EndorsementDeposits -= GetEndorsementDeposit(Block.Protocol, cycle, (int)endorsingRight.Slots);
                    }
                    else if (endorsingRight.Status == BakingRightStatus.Uncovered)
                    {
                        bakerCycle.UncoveredEndorsements -= (int)endorsingRight.Slots;
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
                    bakerCycle.FutureEndorsementRewards += GetFutureEndorsementReward(Block.Protocol, cycle, (int)endorsingRight.Slots);

                    var successReward = GetEndorsementReward(Block.Protocol, (int)endorsingRight.Slots, prevBlock.Priority);

                    var prevRights = prevBakingRights
                        .Where(x => x.Type == BakingRightType.Baking && x.BakerId == rights.Key)
                        .OrderBy(x => x.Priority)
                        .ToList();

                    var maxReward = prevRights.FirstOrDefault()?.Status > BakingRightStatus.Realized
                        ? GetEndorsementReward(Block.Protocol, (int)endorsingRight.Slots, (int)prevRights[0].Priority)
                        : successReward;

                    if (endorsingRight.Status == BakingRightStatus.Realized)
                        bakerCycle.EndorsementRewards -= successReward;
                    else if (endorsingRight.Status == BakingRightStatus.Missed)
                        bakerCycle.MissedEndorsementRewards -= successReward;
                    else if (endorsingRight.Status == BakingRightStatus.Uncovered)
                        bakerCycle.UncoveredEndorsementRewards -= successReward;
                    else
                        throw new Exception("Unexpected future rights");

                    if (maxReward != successReward)
                    {
                        var prevBakerCycle = await Cache.BakerCycles.GetAsync((prevBlock.Level - 1) / Block.Protocol.BlocksPerCycle, rights.Key);
                        Db.TryAttach(prevBakerCycle);

                        if (prevRights[0].Status == BakingRightStatus.Missed)
                        {
                            if (prevRights[0].Priority == 0)
                                prevBakerCycle.MissedOwnBlockRewards -= maxReward - successReward;
                            else
                                prevBakerCycle.MissedExtraBlockRewards -= maxReward - successReward;
                        }
                        else
                        {
                            if (prevRights[0].Priority == 0)
                                prevBakerCycle.UncoveredOwnBlockRewards -= maxReward - successReward;
                            else
                                prevBakerCycle.UncoveredExtraBlockRewards -= maxReward - successReward;
                        }
                    }
                }
                #endregion

                #region baking rewards
                if (bakingRights.Count > 0)
                {
                    if (bakingRights[0].Priority == 0)
                        bakerCycle.FutureBlockRewards += GetFutureBlockReward(Block.Protocol, cycle);

                    var successReward = GetBlockReward(Block.Protocol, (int)bakingRights[0].Priority, Block.Validations);

                    var actualReward = bakingRights[^1].Status == BakingRightStatus.Realized
                        ? GetBlockReward(Block.Protocol, (int)bakingRights[^1].Priority, Block.Validations)
                        : 0;

                    //var maxReward = endorsingRight?.Status > BakingRightStatus.Realized
                    //    ? GetBlockReward(Block.Protocol, (int)bakingRights[0].Priority, Block.Validations + (int)endorsingRight.Slots)
                    //    : successReward;

                    if (actualReward > 0)
                    {
                        if (bakingRights[^1].Priority == 0)
                            bakerCycle.OwnBlockRewards -= actualReward;
                        else
                            bakerCycle.ExtraBlockRewards -= actualReward;
                    }

                    if (successReward != actualReward)
                    {
                        if (bakingRights[0].Status == BakingRightStatus.Missed)
                        {
                            if (bakingRights[0].Priority == 0)
                                bakerCycle.MissedOwnBlockRewards -= successReward - actualReward;
                            else
                                bakerCycle.MissedExtraBlockRewards -= successReward - actualReward;
                        }
                        else
                        {
                            if (bakingRights[0].Priority == 0)
                                bakerCycle.UncoveredOwnBlockRewards -= successReward - actualReward;
                            else
                                bakerCycle.UncoveredExtraBlockRewards -= successReward - actualReward;
                        }
                    }

                    //if (maxReward != successReward)
                    //{
                    //    if (endorsingRight.Status == BakingRightStatus.Missed)
                    //        bakerCycle.MissedEndorsementRewards -= maxReward - successReward;
                    //    else
                    //        bakerCycle.UncoveredEndorsementRewards -= maxReward - successReward;
                    //}
                }
                #endregion

                #region fees
                if (bakingRights.Count > 0)
                {
                    if (bakingRights[^1].Status == BakingRightStatus.Realized)
                    {
                        if (bakingRights[^1].Priority == 0)
                            bakerCycle.OwnBlockFees -= Block.Fees;
                        else
                            bakerCycle.ExtraBlockFees -= Block.Fees;
                    }
                    else if (bakingRights[0].Status == BakingRightStatus.Missed)
                    {
                        if (bakingRights[0].Priority == 0)
                            bakerCycle.MissedOwnBlockFees -= Block.Fees;
                        else
                            bakerCycle.MissedExtraBlockFees -= Block.Fees;
                    }
                    else if (bakingRights[0].Status == BakingRightStatus.Uncovered)
                    {
                        if (bakingRights[0].Priority == 0)
                            bakerCycle.UncoveredOwnBlockFees -= Block.Fees;
                        else
                            bakerCycle.UncoveredExtraBlockFees -= Block.Fees;
                    }
                    else
                    {
                        throw new Exception("Unexpected future rights");
                    }
                }
                #endregion
            }

            if (Block.DoubleBakings != null)
            {
                foreach (var op in Block.DoubleBakings)
                {
                    var offenderCycle = await Cache.BakerCycles.GetAsync((op.AccusedLevel - 1) / Block.Protocol.BlocksPerCycle, op.OffenderId);
                    Db.TryAttach(offenderCycle);

                    offenderCycle.AccusationLostDeposits -= op.OffenderLostDeposit;
                    offenderCycle.AccusationLostRewards -= op.OffenderLostReward;
                    offenderCycle.AccusationLostFees -= op.OffenderLostFee;

                    var accuserCycle = await Cache.BakerCycles.GetAsync(cycle, op.AccuserId);
                    Db.TryAttach(accuserCycle);

                    accuserCycle.AccusationRewards -= op.AccuserReward;
                }
            }

            if (Block.DoubleEndorsings != null)
            {
                foreach (var op in Block.DoubleEndorsings)
                {
                    var offenderCycle = await Cache.BakerCycles.GetAsync((op.AccusedLevel - 1) / Block.Protocol.BlocksPerCycle, op.OffenderId);
                    Db.TryAttach(offenderCycle);

                    offenderCycle.AccusationLostDeposits -= op.OffenderLostDeposit;
                    offenderCycle.AccusationLostRewards -= op.OffenderLostReward;
                    offenderCycle.AccusationLostFees -= op.OffenderLostFee;

                    var accuserCycle = await Cache.BakerCycles.GetAsync(cycle, op.AccuserId);
                    Db.TryAttach(accuserCycle);

                    accuserCycle.AccusationRewards -= op.AccuserReward;
                }
            }

            if (Block.Revelations != null)
            {
                foreach (var op in Block.Revelations)
                {
                    var bakerCycle = await Cache.BakerCycles.GetAsync(cycle, op.BakerId);
                    Db.TryAttach(bakerCycle);

                    bakerCycle.RevelationRewards -= Block.Protocol.RevelationReward;
                }
            }

            if (Block.RevelationPenalties != null)
            {
                foreach (var op in Block.RevelationPenalties)
                {
                    var penaltyCycle = await Cache.BakerCycles.GetAsync((op.MissedLevel - 1) / Block.Protocol.BlocksPerCycle, op.BakerId);
                    Db.TryAttach(penaltyCycle);

                    penaltyCycle.RevelationLostRewards -= op.LostReward;
                    penaltyCycle.RevelationLostFees -= op.LostFees;
                }
            }
            #endregion

            #region new cycle
            if (Block.Events.HasFlag(BlockEvents.CycleBegin))
            {
                await Db.Database.ExecuteSqlRawAsync($@"
                    DELETE  FROM ""BakerCycles""
                    WHERE   ""Cycle"" = {cycle + Block.Protocol.PreservedCycles}");
            }
            #endregion
        }

        #region helpers
        //TODO: figure out how to avoid hardcoded constants for future cycles

        long GetBlockDeposit(Protocol protocol, int cycle)
            => cycle < 64 ? cycle * 8_000_000L : 512_000_000L;

        long GetEndorsementDeposit(Protocol protocol, int cycle, int slots)
            => slots * (cycle < 64 ? cycle * 1_000_000L : 64_000_000L);

        long GetFutureBlockReward(Protocol protocol, int cycle)
            => cycle < protocol.PreservedCycles + 2 ? 0 : 16_000_000L;

        long GetFutureEndorsementReward(Protocol protocol, int cycle, int slots)
            => cycle < protocol.PreservedCycles + 2 ? 0 : slots * 2_000_000L;

        long GetBlockReward(Protocol protocol, int priority, int slots)
            => protocol.BlockReward0;

        long GetEndorsementReward(Protocol protocol, int slots, int prevPriority)
            => slots * (long)(protocol.EndorsementReward0 / (prevPriority + 1.0));
        #endregion

        #region static
        public static async Task<BakerCycleCommit> Apply(
            ProtocolHandler proto,
            Block block,
            Cycle futureCycle,
            List<RawBakingRight> bakingRights,
            List<RawEndorsingRight> endorsingRights,
            Dictionary<int, CycleCommit.DelegateSnapshot> snapshots,
            List<BakingRight> currentRights)
        {
            var commit = new BakerCycleCommit(proto)
            {
                Block = block,
                FutureBakingRights = bakingRights,
                FutureEndorsingRights = endorsingRights,
                FutureCycle = futureCycle,
                Snapshots = snapshots,
                CurrentRights = currentRights
            };
            await commit.Apply();
            return commit;
        }

        public static async Task<BakerCycleCommit> Revert(ProtocolHandler proto, Block block)
        {
            var commit = new BakerCycleCommit(proto) { Block = block };
            await commit.Revert();
            return commit;
        }
        #endregion
    }
}
