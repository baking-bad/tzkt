using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto1
{
    class BakerCycleCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public virtual async Task Apply(
            Block block,
            Cycle? futureCycle,
            IEnumerable<JsonElement>? futureBakingRights,
            IEnumerable<JsonElement>? futureAttestationRights,
            List<SnapshotBalance>? snapshots,
            List<BakingRight> currentRights)
        {
            #region current rights
            var prevBlock = await Cache.Blocks.CurrentAsync();
            var prevBakingRights = prevBlock.Level == 1 ? [] 
                : await Cache.BakingRights.GetAsync(prevBlock.Level);

            foreach (var rights in currentRights.GroupBy(x => x.BakerId))
            {
                var bakerCycle = await Cache.BakerCycles.GetAsync(block.Cycle, rights.Key);
                Db.TryAttach(bakerCycle);

                var bakingRights = rights
                    .Where(x => x.Type == BakingRightType.Baking)
                    .OrderBy(x => x.Round)
                    .ToList();

                var attestationRight = rights
                    .FirstOrDefault(x => x.Type == BakingRightType.Attestation);

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

                if (attestationRight != null)
                {
                    if (bakerCycle.FutureAttestations != 0) // FutureAttestations is always 0 for weirds
                    {
                        bakerCycle.FutureAttestations -= attestationRight.Slots!.Value;
                    }

                    if (attestationRight.Status == BakingRightStatus.Realized)
                    {
                        bakerCycle.Attestations += attestationRight.Slots!.Value;
                    }
                    else if (attestationRight.Status == BakingRightStatus.Missed)
                    {
                        bakerCycle.MissedAttestations += attestationRight.Slots!.Value;
                    }
                    else
                    {
                        throw new Exception("Unexpected future rights");
                    }
                }
                #endregion

                #region attestation rewards
                if (attestationRight != null)
                {
                    if (bakerCycle.FutureAttestationRewards != 0) // FutureAttestationRewards is always 0 for weirds
                        bakerCycle.FutureAttestationRewards -= GetFutureAttestationReward(Context.Protocol, block.Cycle, attestationRight.Slots!.Value);

                    var successReward = GetAttestationReward(Context.Protocol, block.Cycle, attestationRight.Slots!.Value, prevBlock.BlockRound);

                    var prevRights = prevBakingRights
                        .Where(x => x.Type == BakingRightType.Baking && x.BakerId == rights.Key)
                        .OrderBy(x => x.Round)
                        .ToList();

                    var maxReward = prevRights.FirstOrDefault()?.Status > BakingRightStatus.Realized
                        ? GetAttestationReward(Context.Protocol, block.Cycle, attestationRight.Slots.Value, prevRights[0].Round!.Value)
                        : successReward;

                    if (attestationRight.Status == BakingRightStatus.Realized)
                        bakerCycle.AttestationRewardsDelegated += successReward;
                    else if (attestationRight.Status == BakingRightStatus.Missed)
                        bakerCycle.MissedAttestationRewards += successReward;
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
                        bakerCycle.FutureBlockRewards -= GetFutureBlockReward(Context.Protocol, block.Cycle);

                    var successReward = GetBlockReward(Context.Protocol, block.Cycle);
                    
                    var actualReward = bakingRights[^1].Status == BakingRightStatus.Realized
                        ? GetBlockReward(Context.Protocol, block.Cycle)
                        : 0;

                    //var maxReward = attestationRight?.Status > BakingRightStatus.Realized
                    //    ? GetBlockReward(Context.Protocol, (int)bakingRights[0].Round, block.Validations + attestationRight.Slots.Value)
                    //    : successReward;

                    if (actualReward > 0)
                    {
                        bakerCycle.BlockRewardsDelegated += actualReward;
                    }

                    if (successReward != actualReward)
                    {
                        bakerCycle.MissedBlockRewards += successReward - actualReward;
                    }

                    //if (maxReward != successReward)
                    //{
                    //    bakerCycle.MissedAttestationRewards += maxReward - successReward;
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

            foreach (var op in Context.DoubleBakingOps)
            {
                var accusedBlock = await Cache.Blocks.GetAsync(op.AccusedLevel);
                var offenderCycle = await Cache.BakerCycles.GetAsync(accusedBlock.Cycle, op.OffenderId);
                Db.TryAttach(offenderCycle);

                offenderCycle.DoubleBakingLostStaked += op.LostStaked;

                var accuserCycle = await Cache.BakerCycles.GetAsync(block.Cycle, op.AccuserId);
                Db.TryAttach(accuserCycle);

                accuserCycle.DoubleBakingRewards += op.Reward;
            }

            foreach (var op in Context.DoubleConsensusOps)
            {
                var accusedBlock = await Cache.Blocks.GetAsync(op.AccusedLevel);
                var offenderCycle = await Cache.BakerCycles.GetAsync(accusedBlock.Cycle, op.OffenderId);
                Db.TryAttach(offenderCycle);

                offenderCycle.DoubleConsensusLostStaked += op.LostStaked;

                var accuserCycle = await Cache.BakerCycles.GetAsync(block.Cycle, op.AccuserId);
                Db.TryAttach(accuserCycle);

                accuserCycle.DoubleConsensusRewards += op.Reward;
            }

            foreach (var op in Context.NonceRevelationOps)
            {
                var bakerCycle = await Cache.BakerCycles.GetAsync(block.Cycle, op.BakerId);
                Db.TryAttach(bakerCycle);

                bakerCycle.NonceRevelationRewardsDelegated += op.RewardDelegated;
            }

            foreach (var op in Context.RevelationPenaltyOps)
            {
                var penaltyBlock = await Cache.Blocks.GetAsync(op.MissedLevel);
                var penaltyCycle = await Cache.BakerCycles.GetAsync(penaltyBlock.Cycle, op.BakerId);
                Db.TryAttach(penaltyCycle);

                penaltyCycle.NonceRevelationLosses += op.Loss;
            }
            #endregion

            #region new cycle
            if (block.Events.HasFlag(BlockEvents.CycleBegin))
            {
                var bakerCycles = new Dictionary<string, BakerCycle>(snapshots!.Count);
                foreach (var snapshot in snapshots)
                {
                    var baker = await Cache.Accounts.GetAsync(snapshot.AccountId); // WTF: rights were given to non-baker accounts

                    var bakingPower = snapshot.StakingBalance - snapshot.StakingBalance % Context.Protocol.MinimalStake;
                    var share = (double)bakingPower / futureCycle!.TotalBakingPower;

                    var bakerCycle = new BakerCycle
                    {
                        Id = 0,
                        Cycle = futureCycle.Index,
                        BakerId = snapshot.AccountId,
                        OwnDelegatedBalance = snapshot.OwnDelegatedBalance,
                        ExternalDelegatedBalance = snapshot.ExternalDelegatedBalance!.Value,
                        DelegatorsCount = snapshot.DelegatorsCount!.Value,
                        OwnStakedBalance = snapshot.OwnStakedBalance ?? 0,
                        ExternalStakedBalance = snapshot.ExternalStakedBalance ?? 0,
                        StakersCount = snapshot.StakersCount ?? 0,
                        IssuedPseudotokens = snapshot.Pseudotokens,
                        BakingPower = bakingPower,
                        TotalBakingPower = futureCycle.TotalBakingPower,
                        ExpectedBlocks = Context.Protocol.BlocksPerCycle * share,
                        ExpectedAttestations = Context.Protocol.AttestersPerBlock * Context.Protocol.BlocksPerCycle * share
                    };

                    bakerCycles.Add(baker.Address, bakerCycle);
                }

                #region future baking rights
                foreach (var br in futureBakingRights!)
                {
                    if (br.RequiredInt32("priority") > 0)
                        continue;

                    if (!bakerCycles.TryGetValue(br.RequiredString("delegate"), out var bakerCycle))
                        throw new Exception("Nonexistent baker cycle");

                    if (!Cache.Accounts.DelegateExists(br.RequiredString("delegate")))
                        continue;

                    bakerCycle.FutureBlocks++;
                    bakerCycle.FutureBlockRewards += GetFutureBlockReward(Context.Protocol, futureCycle!.Index);
                }
                #endregion

                #region future attestation rights
                var skipLevel = futureAttestationRights!.Last().RequiredInt32("level");

                foreach (var ar in futureAttestationRights!.TakeWhile(x => x.RequiredInt32("level") < skipLevel))
                {
                    if (!bakerCycles.TryGetValue(ar.RequiredString("delegate"), out var bakerCycle))
                        throw new Exception("Nonexistent baker cycle");

                    if (!Cache.Accounts.DelegateExists(ar.RequiredString("delegate")))
                        continue;

                    var slots = ar.RequiredArray("slots").Count();

                    bakerCycle.FutureAttestations += slots;
                    bakerCycle.FutureAttestationRewards += GetFutureAttestationReward(Context.Protocol, futureCycle!.Index, slots);
                }
                #endregion

                #region shifted future attestation rights
                // TODO: cache shifted rights
                var shiftedRights = await Db.BakingRights.AsNoTracking()
                    .Where(x => x.Level == futureCycle!.FirstLevel && x.Type == BakingRightType.Attestation)
                    .ToListAsync();

                foreach (var ar in shiftedRights)
                {
                    if (!Cache.Accounts.DelegateExists(ar.BakerId))
                        continue;

                    var baker = Cache.Accounts.GetDelegate(ar.BakerId);
                    if (!bakerCycles.TryGetValue(baker.Address, out var bakerCycle))
                    {
                        #region shifting hack
                        //shifting is actually a bad idea, but this is the lesser of two evils while Tezos protocol has bugs in the freezer.
                        var snapshottedBaker = await Proto.Rpc.GetDelegateAsync(futureCycle!.SnapshotLevel, baker.Address);
                        var delegators = snapshottedBaker
                            .RequiredArray("delegated_contracts")
                            .EnumerateArray()
                            .Select(x => x.RequiredString())
                            .Where(x => x != baker.Address);

                        if (snapshottedBaker.RequiredInt32("grace_period") != block.Cycle - 3)
                            throw new Exception("Deactivated baker got baking rights");

                        var stakingBalance = snapshottedBaker.RequiredInt64("staking_balance");
                        var delegatedBalance = snapshottedBaker.RequiredInt64("delegated_balance");

                        bakerCycle = new BakerCycle
                        {
                            Id = 0,
                            Cycle = futureCycle.Index,
                            BakerId = baker.Id,
                            OwnDelegatedBalance = stakingBalance - delegatedBalance,
                            ExternalDelegatedBalance = delegatedBalance,
                            DelegatorsCount = delegators.Count(),
                            OwnStakedBalance = 0,
                            ExternalStakedBalance = 0,
                            StakersCount = 0,
                            IssuedPseudotokens = null,
                            BakingPower = 0,
                            TotalBakingPower = futureCycle.TotalBakingPower,
                            ExpectedBlocks = 0,
                            ExpectedAttestations = 0
                        };
                        bakerCycles.Add(baker.Address, bakerCycle);

                        foreach (var delegatorAddress in delegators)
                        {
                            var snapshottedDelegator = await Proto.Rpc.GetContractAsync(futureCycle.SnapshotLevel, delegatorAddress);
                            Db.DelegatorCycles.Add(new DelegatorCycle
                            {
                                Id = 0,
                                Cycle = futureCycle.Index,
                                DelegatorId = (await Cache.Accounts.GetExistingAsync(delegatorAddress)).Id,
                                BakerId = baker.Id,
                                DelegatedBalance = snapshottedDelegator.RequiredInt64("balance"),
                                StakedPseudotokens = null
                            });
                        }
                        #endregion
                    }

                    bakerCycle.FutureAttestations += ar.Slots!.Value;
                    bakerCycle.FutureAttestationRewards += GetFutureAttestationReward(Context.Protocol, futureCycle!.Index, (int)ar.Slots);
                }
                #endregion

                Db.BakerCycles.AddRange(bakerCycles.Values);

                #region weird bakers
                if (block.Cycle > 0)
                {
                    //one-way change...
                    await Db.Database.ExecuteSqlRawAsync("""
                        DELETE FROM "BakerCycles" as bc
                        USING "Accounts" as acc
                        WHERE acc."Id" = bc."BakerId"
                        AND bc."Cycle" = {0}
                        AND acc."Type" != {1}
                        """, block.Cycle - 1, (int)AccountType.Delegate);
                }
                #endregion
            }
            #endregion
        }

        public virtual async Task Revert(Block block)
        {
            #region current rights
            var prevBlock = await Cache.Blocks.PreviousAsync();
            var prevBakingRights = prevBlock.Level == 1 ? []
                : await Cache.BakingRights.GetAsync(prevBlock.Level);

            var currentRights = await Cache.BakingRights.GetAsync(block.Level);

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

                var attestationRight = rights
                    .FirstOrDefault(x => x.Type == BakingRightType.Attestation);

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

                if (attestationRight != null)
                {
                    bakerCycle.FutureAttestations += attestationRight.Slots!.Value;

                    if (attestationRight.Status == BakingRightStatus.Realized)
                    {
                        bakerCycle.Attestations -= attestationRight.Slots.Value;
                    }
                    else if (attestationRight.Status == BakingRightStatus.Missed)
                    {
                        bakerCycle.MissedAttestations -= attestationRight.Slots.Value;
                    }
                    else
                    {
                        throw new Exception("Unexpected future rights");
                    }
                }
                #endregion

                #region attestation rewards
                if (attestationRight != null)
                {
                    bakerCycle.FutureAttestationRewards += GetFutureAttestationReward(Context.Protocol, block.Cycle, attestationRight.Slots!.Value);

                    var successReward = GetAttestationReward(Context.Protocol, block.Cycle, attestationRight.Slots.Value, prevBlock.BlockRound);

                    var prevRights = prevBakingRights
                        .Where(x => x.Type == BakingRightType.Baking && x.BakerId == rights.Key)
                        .OrderBy(x => x.Round)
                        .ToList();

                    var maxReward = prevRights.FirstOrDefault()?.Status > BakingRightStatus.Realized
                        ? GetAttestationReward(Context.Protocol, block.Cycle, attestationRight.Slots.Value, prevRights[0].Round!.Value)
                        : successReward;

                    if (attestationRight.Status == BakingRightStatus.Realized)
                        bakerCycle.AttestationRewardsDelegated -= successReward;
                    else if (attestationRight.Status == BakingRightStatus.Missed)
                        bakerCycle.MissedAttestationRewards -= successReward;
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
                        bakerCycle.FutureBlockRewards += GetFutureBlockReward(Context.Protocol, block.Cycle);

                    var successReward = GetBlockReward(Context.Protocol, block.Cycle);

                    var actualReward = bakingRights[^1].Status == BakingRightStatus.Realized
                        ? GetBlockReward(Context.Protocol, block.Cycle)
                        : 0;

                    //var maxReward = attestationRight?.Status > BakingRightStatus.Realized
                    //    ? GetBlockReward(Context.Protocol, bakingRights[0].Round!.Value, block.Validations + attestationRight.Slots.Value)
                    //    : successReward;

                    if (actualReward > 0)
                    {
                        bakerCycle.BlockRewardsDelegated -= actualReward;
                    }

                    if (successReward != actualReward)
                    {
                        bakerCycle.MissedBlockRewards -= successReward - actualReward;
                    }

                    //if (maxReward != successReward)
                    //{
                    //    bakerCycle.MissedAttestationRewards -= maxReward - successReward;
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

            foreach (var op in Context.DoubleBakingOps)
            {
                var accusedBlock = await Cache.Blocks.GetAsync(op.AccusedLevel);
                var offenderCycle = await Cache.BakerCycles.GetAsync(accusedBlock.Cycle, op.OffenderId);
                Db.TryAttach(offenderCycle);

                offenderCycle.DoubleBakingLostStaked -= op.LostStaked;

                var accuserCycle = await Cache.BakerCycles.GetAsync(block.Cycle, op.AccuserId);
                Db.TryAttach(accuserCycle);

                accuserCycle.DoubleBakingRewards -= op.Reward;
            }

            foreach (var op in Context.DoubleConsensusOps)
            {
                var accusedBlock = await Cache.Blocks.GetAsync(op.AccusedLevel);
                var offenderCycle = await Cache.BakerCycles.GetAsync(accusedBlock.Cycle, op.OffenderId);
                Db.TryAttach(offenderCycle);

                offenderCycle.DoubleConsensusLostStaked -= op.LostStaked;

                var accuserCycle = await Cache.BakerCycles.GetAsync(block.Cycle, op.AccuserId);
                Db.TryAttach(accuserCycle);

                accuserCycle.DoubleConsensusRewards -= op.Reward;
            }

            foreach (var op in Context.NonceRevelationOps)
            {
                var bakerCycle = await Cache.BakerCycles.GetAsync(block.Cycle, op.BakerId);
                Db.TryAttach(bakerCycle);

                bakerCycle.NonceRevelationRewardsDelegated -= op.RewardDelegated;
            }

            foreach (var op in Context.RevelationPenaltyOps)
            {
                var penaltyBlock = await Cache.Blocks.GetAsync(op.MissedLevel);
                var penaltyCycle = await Cache.BakerCycles.GetAsync(penaltyBlock.Cycle, op.BakerId);
                Db.TryAttach(penaltyCycle);

                penaltyCycle.NonceRevelationLosses -= op.Loss;
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

        #region helpers
        protected long GetFutureBlockReward(Protocol protocol, int cycle)
            => cycle < protocol.NoRewardCycles ? 0 : protocol.BlockReward0;

        protected long GetFutureAttestationReward(Protocol protocol, int cycle, int slots)
            => cycle < protocol.NoRewardCycles ? 0 : (slots * protocol.AttestationReward0);

        protected long GetBlockReward(Protocol protocol, int cycle)
            => cycle < protocol.NoRewardCycles ? 0 : protocol.BlockReward0;

        protected long GetAttestationReward(Protocol protocol, int cycle, int slots, int prevPriority)
            => cycle < protocol.NoRewardCycles ? 0 : (slots * (long)(protocol.AttestationReward0 / (prevPriority + 1.0)));
        #endregion
    }
}
