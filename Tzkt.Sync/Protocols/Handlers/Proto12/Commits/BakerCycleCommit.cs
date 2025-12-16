using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto12
{
    class BakerCycleCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public virtual async Task Apply(
            Block block,
            Cycle? futureCycle,
            IEnumerable<RightsGenerator.BR>? futureBakingRights,
            IEnumerable<RightsGenerator.AR>? futureAttestationRights,
            List<SnapshotBalance>? snapshots,
            Dictionary<int, long>? selectedStakes,
            List<BakingRight> currentRights)
        {
            #region current rights
            if (block.BlockRound == 0)
            {
                var bakerCycle = await Cache.BakerCycles.GetAsync(block.Cycle, block.ProposerId!.Value);
                Db.TryAttach(bakerCycle);

                bakerCycle.FutureBlocks--;
                bakerCycle.FutureBlockRewards -= Context.Protocol.MaxBakingReward;
                bakerCycle.Blocks++;
                bakerCycle.BlockRewardsDelegated += block.RewardDelegated + block.BonusDelegated;
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
                            bakerCycle.FutureBlocks--;
                            bakerCycle.FutureBlockRewards -= Context.Protocol.MaxBakingReward;
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
                            bakerCycle.BlockRewardsDelegated += block.RewardDelegated;
                            bakerCycle.BlockFees += block.Fees;
                        }
                        else if (br.Round < block.PayloadRound)
                        {
                            bakerCycle.MissedBlockRewards += block.RewardDelegated;
                            bakerCycle.MissedBlockFees += block.Fees;
                        }

                        if (br.BakerId == block.ProducerId)
                        {
                            bakerCycle.BlockRewardsDelegated += block.BonusDelegated;
                        }
                        else
                        {
                            bakerCycle.MissedBlockRewards += block.BonusDelegated;
                        }
                    }
                }
            }

            foreach (var ar in currentRights.Where(x => x.Type == BakingRightType.Attestation))
            {
                var bakerCycle = await Cache.BakerCycles.GetOrDefaultAsync(block.Cycle, ar.BakerId);
                if (bakerCycle == null) continue;

                Db.TryAttach(bakerCycle);
                bakerCycle.FutureAttestations -= ar.Slots!.Value;
                if (ar.Status == BakingRightStatus.Realized)
                    bakerCycle.Attestations += ar.Slots.Value;
                else if (ar.Status == BakingRightStatus.Missed)
                    bakerCycle.MissedAttestations += ar.Slots.Value;
                else
                    throw new Exception("Unexpected future rights");
            }

            foreach (var op in Context.DoubleBakingOps)
            {
                var offenderCycle = await Cache.BakerCycles.GetOrDefaultAsync(block.Cycle, op.OffenderId);
                if (offenderCycle != null)
                {
                    Db.TryAttach(offenderCycle);
                    offenderCycle.DoubleBakingLostStaked += op.LostStaked;
                }

                var accuserCycle = await Cache.BakerCycles.GetOrDefaultAsync(block.Cycle, op.AccuserId);
                if (accuserCycle != null)
                {
                    Db.TryAttach(accuserCycle);
                    accuserCycle.DoubleBakingRewards += op.Reward;
                }
            }

            foreach (var op in Context.DoubleConsensusOps)
            {
                var offenderCycle = await Cache.BakerCycles.GetOrDefaultAsync(block.Cycle, op.OffenderId);
                if (offenderCycle != null)
                {
                    Db.TryAttach(offenderCycle);
                    offenderCycle.DoubleConsensusLostStaked += op.LostStaked;
                }

                var accuserCycle = await Cache.BakerCycles.GetOrDefaultAsync(block.Cycle, op.AccuserId);
                if (accuserCycle != null)
                {
                    Db.TryAttach(accuserCycle);
                    accuserCycle.DoubleConsensusRewards += op.Reward;
                }
            }

            foreach (var op in Context.NonceRevelationOps)
            {
                var bakerCycle = await Cache.BakerCycles.GetOrDefaultAsync(block.Cycle, op.BakerId);
                if (bakerCycle != null)
                {
                    Db.TryAttach(bakerCycle);
                    bakerCycle.NonceRevelationRewardsDelegated += op.RewardDelegated;
                }
            }

            foreach (var op in Context.VdfRevelationOps)
            {
                var bakerCycle = await Cache.BakerCycles.GetAsync(block.Cycle, op.BakerId);
                Db.TryAttach(bakerCycle);

                bakerCycle.VdfRevelationRewardsDelegated += op.RewardDelegated;
            }
            #endregion

            #region new cycle
            if (block.Events.HasFlag(BlockEvents.CycleBegin))
            {
                var bakerCycles = snapshots!.ToDictionary(x => x.AccountId, snapshot =>
                {
                    var bakerCycle = new BakerCycle
                    {
                        Id = 0,
                        BakerId = snapshot.AccountId,
                        Cycle = futureCycle!.Index,
                        OwnDelegatedBalance = snapshot.OwnDelegatedBalance,
                        ExternalDelegatedBalance = snapshot.ExternalDelegatedBalance!.Value,
                        DelegatorsCount = snapshot.DelegatorsCount!.Value,
                        OwnStakedBalance = snapshot.OwnStakedBalance ?? 0,
                        ExternalStakedBalance = snapshot.ExternalStakedBalance ?? 0,
                        StakersCount = snapshot.StakersCount ?? 0,
                        IssuedPseudotokens = snapshot.Pseudotokens,
                        BakingPower = 0,
                        TotalBakingPower = futureCycle.TotalBakingPower
                    };
                    if (selectedStakes!.TryGetValue(bakerCycle.BakerId, out var bakingPower))
                    {
                        var expectedAttestations = (Context.Protocol.BlocksPerCycle * Context.Protocol.AttestersPerBlock).MulRatio(bakingPower, futureCycle.TotalBakingPower);
                        bakerCycle.BakingPower = bakingPower;
                        bakerCycle.ExpectedBlocks = Context.Protocol.BlocksPerCycle.MulRatio(bakingPower, futureCycle.TotalBakingPower);
                        bakerCycle.ExpectedAttestations = expectedAttestations;
                        bakerCycle.FutureAttestationRewards = expectedAttestations * Context.Protocol.AttestationReward0;
                    }
                    return bakerCycle;
                });

                #region future baking rights
                foreach (var br in futureBakingRights!.Where(x => x.Round == 0))
                {
                    if (!bakerCycles.TryGetValue(br.Baker, out var bakerCycle))
                        throw new Exception("Nonexistent baker cycle");

                    bakerCycle.FutureBlocks++;
                    bakerCycle.FutureBlockRewards += Context.Protocol.MaxBakingReward;
                }
                #endregion

                #region future attestation rights
                var skipLevel = futureAttestationRights!.Last().Level;
                foreach (var ar in futureAttestationRights!.TakeWhile(x => x.Level < skipLevel))
                {
                    if (!bakerCycles.TryGetValue(ar.Baker, out var bakerCycle))
                        throw new Exception("Nonexistent baker cycle");

                    bakerCycle.FutureAttestations += ar.Slots;
                }
                #endregion

                #region shifted future attestation rights
                var shifted = await Db.BakingRights.AsNoTracking()
                    .Where(x => x.Level == futureCycle!.FirstLevel && x.Type == BakingRightType.Attestation)
                    .ToListAsync();

                foreach (var ar in shifted)
                {
                    if (bakerCycles.TryGetValue(ar.BakerId, out var bakerCycle))
                    {
                        bakerCycle.FutureAttestations += ar.Slots!.Value;
                    }
                }
                #endregion

                Db.BakerCycles.AddRange(bakerCycles.Values);
            }
            #endregion
        }

        public virtual async Task Revert(Block block)
        {
            #region current rights
            var currentRights = await Cache.BakingRights.GetAsync(block.Level);

            if (block.BlockRound == 0)
            {
                var bakerCycle = await Cache.BakerCycles.GetAsync(block.Cycle, block.ProposerId!.Value);
                Db.TryAttach(bakerCycle);

                bakerCycle.FutureBlocks++;
                bakerCycle.FutureBlockRewards += Context.Protocol.MaxBakingReward;
                bakerCycle.Blocks--;
                bakerCycle.BlockRewardsDelegated -= block.RewardDelegated + block.BonusDelegated;
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
                            bakerCycle.FutureBlocks++;
                            bakerCycle.FutureBlockRewards += Context.Protocol.MaxBakingReward;
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
                            bakerCycle.BlockRewardsDelegated -= block.RewardDelegated;
                            bakerCycle.BlockFees -= block.Fees;
                        }
                        else if (br.Round < bakerRound)
                        {
                            bakerCycle.MissedBlockRewards -= block.RewardDelegated;
                            bakerCycle.MissedBlockFees -= block.Fees;
                        }

                        if (br.BakerId == block.ProducerId)
                        {
                            bakerCycle.BlockRewardsDelegated -= block.BonusDelegated;
                        }
                        else
                        {
                            bakerCycle.MissedBlockRewards -= block.BonusDelegated;
                        }
                    }
                }
            }

            foreach (var ar in currentRights.Where(x => x.Type == BakingRightType.Attestation))
            {
                var bakerCycle = await Cache.BakerCycles.GetOrDefaultAsync(block.Cycle, ar.BakerId);
                if (bakerCycle == null) continue;

                Db.TryAttach(bakerCycle);
                bakerCycle.FutureAttestations += ar.Slots!.Value;
                if (ar.Status == BakingRightStatus.Realized)
                    bakerCycle.Attestations -= ar.Slots.Value;
                else if (ar.Status == BakingRightStatus.Missed)
                    bakerCycle.MissedAttestations -= ar.Slots.Value;
                else
                    throw new Exception("Unexpected future rights");
            }

            foreach (var op in Context.DoubleBakingOps)
            {
                var offenderCycle = await Cache.BakerCycles.GetOrDefaultAsync(block.Cycle, op.OffenderId);
                if (offenderCycle != null)
                {
                    Db.TryAttach(offenderCycle);
                    offenderCycle.DoubleBakingLostStaked -= op.LostStaked;
                }

                var accuserCycle = await Cache.BakerCycles.GetOrDefaultAsync(block.Cycle, op.AccuserId);
                if (accuserCycle != null)
                {
                    Db.TryAttach(accuserCycle);
                    accuserCycle.DoubleBakingRewards -= op.Reward;
                }
            }

            foreach (var op in Context.DoubleConsensusOps)
            {
                var offenderCycle = await Cache.BakerCycles.GetOrDefaultAsync(block.Cycle, op.OffenderId);
                if (offenderCycle != null)
                {
                    Db.TryAttach(offenderCycle);
                    offenderCycle.DoubleConsensusLostStaked -= op.LostStaked;
                }

                var accuserCycle = await Cache.BakerCycles.GetOrDefaultAsync(block.Cycle, op.AccuserId);
                if (accuserCycle != null)
                {
                    Db.TryAttach(accuserCycle);
                    accuserCycle.DoubleConsensusRewards -= op.Reward;
                }
            }

            foreach (var op in Context.NonceRevelationOps)
            {
                var bakerCycle = await Cache.BakerCycles.GetOrDefaultAsync(block.Cycle, op.BakerId);
                if (bakerCycle != null)
                {
                    Db.TryAttach(bakerCycle);
                    bakerCycle.NonceRevelationRewardsDelegated -= op.RewardDelegated;
                }
            }

            foreach (var op in Context.VdfRevelationOps)
            {
                var bakerCycle = await Cache.BakerCycles.GetAsync(block.Cycle, op.BakerId);
                Db.TryAttach(bakerCycle);

                bakerCycle.VdfRevelationRewardsDelegated -= op.RewardDelegated;
            }
            #endregion

            #region new cycle
            if (block.Events.HasFlag(BlockEvents.CycleBegin))
            {
                await Db.Database.ExecuteSqlRawAsync("""
                    DELETE FROM "BakerCycles"
                    WHERE "Cycle" = {0}
                    """, block.Cycle + Context.Protocol.ConsensusRightsDelay);
            }
            #endregion
        }
    }
}
