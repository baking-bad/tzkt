using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto18
{
    class BakerCycleCommit : ProtocolCommit
    {
        public BakerCycleCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual async Task Apply(
            Block block,
            Cycle futureCycle,
            IEnumerable<RightsGenerator.BR> futureBakingRights,
            IEnumerable<RightsGenerator.ER> futureEndorsingRights,
            List<SnapshotBalance> snapshots,
            Dictionary<int, long> selectedStakes,
            List<BakingRight> currentRights)
        {
            #region current rights
            if (block.BlockRound == 0)
            {
                var bakerCycle = await Cache.BakerCycles.GetAsync(block.Cycle, (int)block.ProposerId);
                Db.TryAttach(bakerCycle);

                bakerCycle.FutureBlockRewards -= bakerCycle.FutureBlockRewards / bakerCycle.FutureBlocks;
                bakerCycle.FutureBlocks--;
                bakerCycle.Blocks++;
                bakerCycle.BlockRewardsLiquid += block.RewardLiquid + block.BonusLiquid;
                bakerCycle.BlockRewardsStakedOwn += block.RewardStakedOwn + block.BonusStakedOwn;
                bakerCycle.BlockRewardsStakedShared += block.RewardStakedShared + block.BonusStakedShared;
                bakerCycle.BlockFees += block.Fees;
            }
            else
            {
                var set = new HashSet<int>();
                foreach (var br in currentRights.Where(x => x.Type == BakingRightType.Baking).OrderBy(x => x.Round))
                {
                    if (set.Add(br.BakerId))
                    {
                        var bakerCycle = await Cache.BakerCycles.GetAsync(block.Cycle, br.BakerId);
                        Db.TryAttach(bakerCycle);

                        if (br.Round == 0)
                        {
                            bakerCycle.FutureBlockRewards -= bakerCycle.FutureBlockRewards / bakerCycle.FutureBlocks;
                            bakerCycle.FutureBlocks--;
                        }

                        if (br.BakerId == block.ProposerId || br.BakerId == block.ProducerId)
                        {
                            bakerCycle.Blocks++;
                        }
                        else
                        {
                            bakerCycle.MissedBlocks++;
                        }

                        if (br.BakerId == block.ProposerId)
                        {
                            bakerCycle.BlockRewardsLiquid += block.RewardLiquid;
                            bakerCycle.BlockRewardsStakedOwn += block.RewardStakedOwn;
                            bakerCycle.BlockRewardsStakedShared += block.RewardStakedShared;
                            bakerCycle.BlockFees += block.Fees;
                        }
                        else if (br.Round < block.PayloadRound)
                        {
                            bakerCycle.MissedBlockRewards += block.RewardLiquid + block.RewardStakedOwn + block.RewardStakedShared;
                            bakerCycle.MissedBlockFees += block.Fees;
                        }

                        if (br.BakerId == block.ProducerId)
                        {
                            bakerCycle.BlockRewardsLiquid += block.BonusLiquid;
                            bakerCycle.BlockRewardsStakedOwn += block.BonusStakedOwn;
                            bakerCycle.BlockRewardsStakedShared += block.BonusStakedShared;
                        }
                        else
                        {
                            bakerCycle.MissedBlockRewards += block.BonusLiquid + block.BonusStakedOwn + block.BonusStakedShared;
                        }
                    }
                }
            }

            foreach (var er in currentRights.Where(x => x.Type == BakingRightType.Endorsing))
            {
                var bakerCycle = await Cache.BakerCycles.GetOrDefaultAsync(block.Cycle, er.BakerId);
                if (bakerCycle == null) continue;

                Db.TryAttach(bakerCycle);
                bakerCycle.FutureEndorsements -= (int)er.Slots;
                if (er.Status == BakingRightStatus.Realized)
                    bakerCycle.Endorsements += (int)er.Slots;
                else if (er.Status == BakingRightStatus.Missed)
                    bakerCycle.MissedEndorsements += (int)er.Slots;
                else
                    throw new Exception("Unexpected future rights");
            }

            if (block.DoubleBakings != null)
            {
                foreach (var op in block.DoubleBakings)
                {
                    var offenderCycle = await Cache.BakerCycles.GetOrDefaultAsync(block.Cycle, op.Offender.Id);
                    if (offenderCycle != null)
                    {
                        Db.TryAttach(offenderCycle);
                        offenderCycle.DoubleBakingLossesOwn += op.OffenderLossOwn;
                        offenderCycle.DoubleBakingLossesShared += op.OffenderLossShared;
                    }

                    var accuserCycle = await Cache.BakerCycles.GetOrDefaultAsync(block.Cycle, op.Accuser.Id);
                    if (accuserCycle != null)
                    {
                        Db.TryAttach(accuserCycle);
                        accuserCycle.DoubleBakingRewards += op.AccuserReward;
                    }
                }
            }

            if (block.DoubleEndorsings != null)
            {
                foreach (var op in block.DoubleEndorsings)
                {
                    var offenderCycle = await Cache.BakerCycles.GetOrDefaultAsync(block.Cycle, op.Offender.Id);
                    if (offenderCycle != null)
                    {
                        Db.TryAttach(offenderCycle);
                        offenderCycle.DoubleEndorsingLossesOwn += op.OffenderLossOwn;
                        offenderCycle.DoubleEndorsingLossesShared += op.OffenderLossShared;
                    }

                    var accuserCycle = await Cache.BakerCycles.GetOrDefaultAsync(block.Cycle, op.Accuser.Id);
                    if (accuserCycle != null)
                    {
                        Db.TryAttach(accuserCycle);
                        accuserCycle.DoubleEndorsingRewards += op.AccuserReward;
                    }
                }
            }

            if (block.DoublePreendorsings != null)
            {
                foreach (var op in block.DoublePreendorsings)
                {
                    var offenderCycle = await Cache.BakerCycles.GetOrDefaultAsync(block.Cycle, op.Offender.Id);
                    if (offenderCycle != null)
                    {
                        Db.TryAttach(offenderCycle);
                        offenderCycle.DoublePreendorsingLossesOwn += op.OffenderLossOwn;
                        offenderCycle.DoublePreendorsingLossesShared += op.OffenderLossShared;
                    }

                    var accuserCycle = await Cache.BakerCycles.GetOrDefaultAsync(block.Cycle, op.Accuser.Id);
                    if (accuserCycle != null)
                    {
                        Db.TryAttach(accuserCycle);
                        accuserCycle.DoublePreendorsingRewards += op.AccuserReward;
                    }
                }
            }

            if (block.Revelations != null)
            {
                foreach (var op in block.Revelations)
                {
                    var bakerCycle = await Cache.BakerCycles.GetOrDefaultAsync(block.Cycle, op.Baker.Id);
                    if (bakerCycle != null)
                    {
                        Db.TryAttach(bakerCycle);
                        bakerCycle.NonceRevelationRewardsLiquid += op.RewardLiquid;
                        bakerCycle.NonceRevelationRewardsStakedOwn += op.RewardStakedOwn;
                        bakerCycle.NonceRevelationRewardsStakedShared += op.RewardStakedShared;
                    }
                }
            }

            if (block.VdfRevelationOps != null)
            {
                foreach (var op in block.VdfRevelationOps)
                {
                    var bakerCycle = await Cache.BakerCycles.GetAsync(block.Cycle, op.Baker.Id);
                    Db.TryAttach(bakerCycle);

                    bakerCycle.VdfRevelationRewardsLiquid += op.RewardLiquid;
                    bakerCycle.VdfRevelationRewardsStakedOwn += op.RewardStakedOwn;
                    bakerCycle.VdfRevelationRewardsStakedShared += op.RewardStakedShared;
                }
            }
            #endregion

            #region new cycle
            if (block.Events.HasFlag(BlockEvents.CycleBegin))
            {
                var bakerCycles = snapshots.ToDictionary(x => x.AccountId, snapshot =>
                {
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
                        BakingPower = 0,
                        TotalBakingPower = futureCycle.TotalBakingPower
                    };
                    if (selectedStakes.TryGetValue(bakerCycle.BakerId, out var bakingPower))
                    {
                        var expectedEndorsements = (int)(new BigInteger(block.Protocol.BlocksPerCycle) * block.Protocol.EndorsersPerBlock * bakingPower / futureCycle.TotalBakingPower);
                        bakerCycle.BakingPower = bakingPower;
                        bakerCycle.ExpectedBlocks = block.Protocol.BlocksPerCycle * bakingPower / futureCycle.TotalBakingPower;
                        bakerCycle.ExpectedEndorsements = expectedEndorsements;
                        bakerCycle.FutureEndorsementRewards = expectedEndorsements * futureCycle.EndorsementRewardPerSlot;
                    }
                    return bakerCycle;
                });

                #region future baking rights
                foreach (var br in futureBakingRights.Where(x => x.Round == 0))
                {
                    if (!bakerCycles.TryGetValue(br.Baker, out var bakerCycle))
                        throw new Exception("Nonexistent baker cycle");

                    bakerCycle.FutureBlocks++;
                    bakerCycle.FutureBlockRewards += futureCycle.MaxBlockReward;
                }
                #endregion

                #region future endorsing rights
                var skipLevel = futureEndorsingRights.Last().Level;
                foreach (var er in futureEndorsingRights.TakeWhile(x => x.Level < skipLevel))
                {
                    if (!bakerCycles.TryGetValue(er.Baker, out var bakerCycle))
                        throw new Exception("Nonexistent baker cycle");

                    bakerCycle.FutureEndorsements += er.Slots;
                }
                #endregion

                #region shifted future endorsing rights
                var shifted = await Db.BakingRights.AsNoTracking()
                    .Where(x => x.Level == futureCycle.FirstLevel && x.Type == BakingRightType.Endorsing)
                    .ToListAsync();

                foreach (var er in shifted)
                {
                    if (bakerCycles.TryGetValue(er.BakerId, out var bakerCycle))
                    {
                        bakerCycle.FutureEndorsements += (int)er.Slots;
                    }
                }
                #endregion

                Db.BakerCycles.AddRange(bakerCycles.Values);
            }
            #endregion
        }

        public virtual async Task Revert(Block block)
        {
            block.Protocol ??= await Cache.Protocols.GetAsync(block.ProtoCode);

            #region current rights
            var currentRights = await Cache.BakingRights.GetAsync(block.Cycle, block.Level);
            var currentCycle = await Db.Cycles.SingleAsync(x => x.Index == block.Cycle);

            if (block.BlockRound == 0)
            {
                var bakerCycle = await Cache.BakerCycles.GetAsync(block.Cycle, (int)block.ProposerId);
                Db.TryAttach(bakerCycle);

                bakerCycle.FutureBlockRewards += currentCycle.MaxBlockReward;
                bakerCycle.FutureBlocks++;
                bakerCycle.Blocks--;
                bakerCycle.BlockRewardsLiquid -= block.RewardLiquid + block.BonusLiquid;
                bakerCycle.BlockRewardsStakedOwn -= block.RewardStakedOwn + block.BonusStakedOwn;
                bakerCycle.BlockRewardsStakedShared -= block.RewardStakedShared + block.BonusStakedShared;
                bakerCycle.BlockFees -= block.Fees;
            }
            else
            {
                var set = new HashSet<int>();
                var bakerRound = currentRights
                    .Where(x => x.Type == BakingRightType.Baking)
                    .OrderBy(x => x.Round)
                    .First(x => x.Status == BakingRightStatus.Realized)
                    .Round;

                foreach (var br in currentRights.Where(x => x.Type == BakingRightType.Baking).OrderBy(x => x.Round))
                {
                    if (set.Add(br.BakerId))
                    {
                        var bakerCycle = await Cache.BakerCycles.GetAsync(block.Cycle, br.BakerId);
                        Db.TryAttach(bakerCycle);

                        if (br.Round == 0)
                        {
                            bakerCycle.FutureBlockRewards += currentCycle.MaxBlockReward;
                            bakerCycle.FutureBlocks++;
                        }

                        if (br.BakerId == block.ProposerId || br.BakerId == block.ProducerId)
                        {
                            bakerCycle.Blocks--;
                        }
                        else
                        {
                            bakerCycle.MissedBlocks--;
                        }

                        if (br.BakerId == block.ProposerId)
                        {
                            bakerCycle.BlockRewardsLiquid -= block.RewardLiquid;
                            bakerCycle.BlockRewardsStakedOwn -= block.RewardStakedOwn;
                            bakerCycle.BlockRewardsStakedShared -= block.RewardStakedShared;
                            bakerCycle.BlockFees -= block.Fees;
                        }
                        else if (br.Round < bakerRound)
                        {
                            bakerCycle.MissedBlockRewards -= block.RewardLiquid + block.RewardStakedOwn + block.RewardStakedShared;
                            bakerCycle.MissedBlockFees -= block.Fees;
                        }

                        if (br.BakerId == block.ProducerId)
                        {
                            bakerCycle.BlockRewardsLiquid -= block.BonusLiquid;
                            bakerCycle.BlockRewardsStakedOwn -= block.BonusStakedOwn;
                            bakerCycle.BlockRewardsStakedShared -= block.BonusStakedShared;
                        }
                        else
                        {
                            bakerCycle.MissedBlockRewards -= block.BonusLiquid + block.BonusStakedOwn + block.BonusStakedShared;
                        }
                    }
                }
            }

            foreach (var er in currentRights.Where(x => x.Type == BakingRightType.Endorsing))
            {
                var bakerCycle = await Cache.BakerCycles.GetOrDefaultAsync(block.Cycle, er.BakerId);
                if (bakerCycle == null) continue;

                Db.TryAttach(bakerCycle);
                bakerCycle.FutureEndorsements += (int)er.Slots;
                if (er.Status == BakingRightStatus.Realized)
                    bakerCycle.Endorsements -= (int)er.Slots;
                else if (er.Status == BakingRightStatus.Missed)
                    bakerCycle.MissedEndorsements -= (int)er.Slots;
                else
                    throw new Exception("Unexpected future rights");
            }

            if (block.DoubleBakings != null)
            {
                foreach (var op in block.DoubleBakings)
                {
                    var offenderCycle = await Cache.BakerCycles.GetOrDefaultAsync(block.Cycle, op.OffenderId);
                    if (offenderCycle != null)
                    {
                        Db.TryAttach(offenderCycle);
                        offenderCycle.DoubleBakingLossesOwn -= op.OffenderLossOwn;
                        offenderCycle.DoubleBakingLossesShared -= op.OffenderLossShared;
                    }

                    var accuserCycle = await Cache.BakerCycles.GetOrDefaultAsync(block.Cycle, op.AccuserId);
                    if (accuserCycle != null)
                    {
                        Db.TryAttach(accuserCycle);
                        accuserCycle.DoubleBakingRewards -= op.AccuserReward;
                    }
                }
            }

            if (block.DoubleEndorsings != null)
            {
                foreach (var op in block.DoubleEndorsings)
                {
                    var offenderCycle = await Cache.BakerCycles.GetOrDefaultAsync(block.Cycle, op.OffenderId);
                    if (offenderCycle != null)
                    {
                        Db.TryAttach(offenderCycle);
                        offenderCycle.DoubleEndorsingLossesOwn -= op.OffenderLossOwn;
                        offenderCycle.DoubleEndorsingLossesShared -= op.OffenderLossShared;
                    }

                    var accuserCycle = await Cache.BakerCycles.GetOrDefaultAsync(block.Cycle, op.AccuserId);
                    if (accuserCycle != null)
                    {
                        Db.TryAttach(accuserCycle);
                        accuserCycle.DoubleEndorsingRewards -= op.AccuserReward;
                    }
                }
            }

            if (block.DoublePreendorsings != null)
            {
                foreach (var op in block.DoublePreendorsings)
                {
                    var offenderCycle = await Cache.BakerCycles.GetOrDefaultAsync(block.Cycle, op.OffenderId);
                    if (offenderCycle != null)
                    {
                        Db.TryAttach(offenderCycle);
                        offenderCycle.DoublePreendorsingLossesOwn -= op.OffenderLossOwn;
                        offenderCycle.DoublePreendorsingLossesShared -= op.OffenderLossShared;
                    }

                    var accuserCycle = await Cache.BakerCycles.GetOrDefaultAsync(block.Cycle, op.AccuserId);
                    if (accuserCycle != null)
                    {
                        Db.TryAttach(accuserCycle);
                        accuserCycle.DoublePreendorsingRewards -= op.AccuserReward;
                    }
                }
            }

            if (block.Revelations != null)
            {
                foreach (var op in block.Revelations)
                {
                    var bakerCycle = await Cache.BakerCycles.GetOrDefaultAsync(block.Cycle, op.BakerId);
                    if (bakerCycle != null)
                    {
                        Db.TryAttach(bakerCycle);
                        bakerCycle.NonceRevelationRewardsLiquid -= op.RewardLiquid;
                        bakerCycle.NonceRevelationRewardsStakedOwn -= op.RewardStakedOwn;
                        bakerCycle.NonceRevelationRewardsStakedShared -= op.RewardStakedShared;
                    }
                }
            }

            if (block.VdfRevelationOps != null)
            {
                foreach (var op in block.VdfRevelationOps)
                {
                    var bakerCycle = await Cache.BakerCycles.GetAsync(block.Cycle, op.BakerId);
                    Db.TryAttach(bakerCycle);

                    bakerCycle.VdfRevelationRewardsLiquid -= op.RewardLiquid;
                    bakerCycle.VdfRevelationRewardsStakedOwn -= op.RewardStakedOwn;
                    bakerCycle.VdfRevelationRewardsStakedShared -= op.RewardStakedShared;
                }
            }
            #endregion

            #region new cycle
            if (block.Events.HasFlag(BlockEvents.CycleBegin))
            {
                await Db.Database.ExecuteSqlRawAsync($"""
                    DELETE FROM "BakerCycles"
                    WHERE "Cycle" = {block.Cycle + block.Protocol.PreservedCycles}
                    """);
            }
            #endregion
        }
    }
}
