using System.Numerics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto18
{
    class SlashingCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public async Task Apply(Block block, JsonElement rawBlock)
        {
            if (!block.Events.HasFlag(BlockEvents.CycleEnd))
                return;

            var slashings = rawBlock
                .Required("metadata")
                .RequiredArray("balance_updates")
                .EnumerateArray()
                .Where(x => x.RequiredString("origin") == "delayed_operation");

            if (!slashings.Any())
                return;

            var slashedRequests = await SlashUnstakeRequests(block, slashings);

            foreach (var slashing in slashings.GroupBy(x => x.RequiredString("delayed_operation_hash")).Reverse())
            {
                var opHash = slashing.Key;
                var accusation = Context.DoubleBakingOps.FirstOrDefault(x => x.OpHash == opHash)
                    ?? Context.DoubleEndorsingOps.FirstOrDefault(x => x.OpHash == opHash)
                    ?? Context.DoublePreendorsingOps.FirstOrDefault(x => x.OpHash == opHash)
                    ?? Db.DoubleBakingOps.FirstOrDefault(x => x.OpHash == opHash)
                    ?? Db.DoubleEndorsingOps.FirstOrDefault(x => x.OpHash == opHash)
                    ?? (BaseOperation?)Db.DoublePreendorsingOps.FirstOrDefault(x => x.OpHash == opHash)
                    ?? throw new Exception($"Cannot find delayed operation '{opHash}'");

                var (accuserId, offenderId) = accusation switch
                {
                    DoubleBakingOperation op => (op.AccuserId, op.OffenderId),
                    DoubleEndorsingOperation op => (op.AccuserId, op.OffenderId),
                    DoublePreendorsingOperation op => (op.AccuserId, op.OffenderId),
                    _ => throw new InvalidOperationException()
                };
                var accuser = Cache.Accounts.GetDelegate(accuserId);
                var offender = Cache.Accounts.GetDelegate(offenderId);

                var updates = await ParseStakingUpdates(block, accusation, slashedRequests[opHash], slashing);
                await new StakingUpdateCommit(Proto).Apply(updates);

                var lostOwnStaked = updates
                    .Where(x => x.Type == StakingUpdateType.SlashStaked && x.StakerId == x.BakerId)
                    .Sum(x => x.Amount);
                var lostExternalStaked = updates
                    .Where(x => x.Type == StakingUpdateType.SlashStaked && x.StakerId != x.BakerId)
                    .Sum(x => x.Amount);
                var lostOwnUnstaked = updates
                    .Where(x => x.Type == StakingUpdateType.SlashUnstaked && x.StakerId == x.BakerId)
                    .Sum(x => x.Amount);
                var lostExternalUnstaked = updates
                    .Where(x => x.Type == StakingUpdateType.SlashUnstaked && x.StakerId != x.BakerId)
                    .Sum(x => x.Amount);
                var reward = slashing
                    .Where(x => x.RequiredString("kind") == "contract")
                    .Sum(x => x.RequiredInt64("change"));

                Db.TryAttach(accuser);
                accuser.Balance += reward;
                accuser.StakingBalance += reward;
                accuser.LastLevel = block.Level;

                var accuserCycle = await Cache.BakerCycles.GetOrDefaultAsync(block.Cycle, accuser.Id);
                var offenderCycle = await Cache.BakerCycles.GetOrDefaultAsync(block.Cycle, offender.Id);

                switch (accusation)
                {
                    case DoubleBakingOperation op:
                        op.SlashedLevel = block.Level;
                        op.Reward = reward;
                        op.LostStaked = lostOwnStaked;
                        op.LostUnstaked = lostOwnUnstaked;
                        op.LostExternalStaked = lostExternalStaked;
                        op.LostExternalUnstaked = lostExternalUnstaked;
                        op.StakingUpdatesCount = updates.Count;
                        if (accuserCycle != null)
                        {
                            Db.TryAttach(accuserCycle);
                            accuserCycle.DoubleBakingRewards += reward;
                        }
                        if (offenderCycle != null)
                        {
                            Db.TryAttach(offenderCycle);
                            offenderCycle.DoubleBakingLostStaked += lostOwnStaked;
                            offenderCycle.DoubleBakingLostUnstaked += lostOwnUnstaked;
                            offenderCycle.DoubleBakingLostExternalStaked += lostExternalStaked;
                            offenderCycle.DoubleBakingLostExternalUnstaked += lostExternalUnstaked;
                        }
                        Db.TryAttach(block);
                        block.Events |= BlockEvents.DoubleBakingSlashing;
                        break;
                    case DoubleEndorsingOperation op:
                        op.SlashedLevel = block.Level;
                        op.Reward = reward;
                        op.LostStaked = lostOwnStaked;
                        op.LostUnstaked = lostOwnUnstaked;
                        op.LostExternalStaked = lostExternalStaked;
                        op.LostExternalUnstaked = lostExternalUnstaked;
                        op.StakingUpdatesCount = updates.Count;
                        if (accuserCycle != null)
                        {
                            Db.TryAttach(accuserCycle);
                            accuserCycle.DoubleEndorsingRewards += reward;
                        }
                        if (offenderCycle != null)
                        {
                            Db.TryAttach(offenderCycle);
                            offenderCycle.DoubleEndorsingLostStaked += lostOwnStaked;
                            offenderCycle.DoubleEndorsingLostUnstaked += lostOwnUnstaked;
                            offenderCycle.DoubleEndorsingLostExternalStaked += lostExternalStaked;
                            offenderCycle.DoubleEndorsingLostExternalUnstaked += lostExternalUnstaked;
                        }
                        Db.TryAttach(block);
                        block.Events |= BlockEvents.DoubleEndorsingSlashing;
                        break;
                    case DoublePreendorsingOperation op:
                        op.SlashedLevel = block.Level;
                        op.Reward = reward;
                        op.LostStaked = lostOwnStaked;
                        op.LostUnstaked = lostOwnUnstaked;
                        op.LostExternalStaked = lostExternalStaked;
                        op.LostExternalUnstaked = lostExternalUnstaked;
                        op.StakingUpdatesCount = updates.Count;
                        if (accuserCycle != null)
                        {
                            Db.TryAttach(accuserCycle);
                            accuserCycle.DoublePreendorsingRewards += reward;
                        }
                        if (offenderCycle != null)
                        {
                            Db.TryAttach(offenderCycle);
                            offenderCycle.DoublePreendorsingLostStaked += lostOwnStaked;
                            offenderCycle.DoublePreendorsingLostUnstaked += lostOwnUnstaked;
                            offenderCycle.DoublePreendorsingLostExternalStaked += lostExternalStaked;
                            offenderCycle.DoublePreendorsingLostExternalUnstaked += lostExternalUnstaked;
                        }
                        Db.TryAttach(block);
                        block.Events |= BlockEvents.DoublePreendorsingSlashing;
                        break;
                    default:
                        throw new InvalidOperationException();
                }

                Cache.Statistics.Current.TotalBurned += lostOwnStaked + lostExternalStaked + lostOwnUnstaked + lostExternalUnstaked - reward;
            }
        }

        public async Task Revert(Block block)
        {
            if (block.Events.HasFlag(BlockEvents.DoubleBakingSlashing))
            {
                foreach (var op in await Db.DoubleBakingOps.Where(x => x.SlashedLevel == block.Level).ToListAsync())
                {
                    var accuser = Cache.Accounts.GetDelegate(op.AccuserId);
                    Db.TryAttach(accuser);
                    accuser.Balance -= op.Reward;
                    accuser.StakingBalance -= op.Reward;

                    var accuserCycle = await Cache.BakerCycles.GetOrDefaultAsync(block.Cycle, accuser.Id);
                    if (accuserCycle != null)
                    {
                        Db.TryAttach(accuserCycle);
                        accuserCycle.DoubleBakingRewards -= op.Reward;
                    }

                    var offenderCycle = await Cache.BakerCycles.GetOrDefaultAsync(block.Cycle, op.OffenderId);
                    if (offenderCycle != null)
                    {
                        Db.TryAttach(offenderCycle);
                        offenderCycle.DoubleBakingLostStaked -= op.LostStaked;
                        offenderCycle.DoubleBakingLostUnstaked -= op.LostUnstaked;
                        offenderCycle.DoubleBakingLostExternalStaked -= op.LostExternalStaked;
                        offenderCycle.DoubleBakingLostExternalUnstaked -= op.LostExternalUnstaked;
                    }

                    op.Reward = 0;
                    op.LostStaked = 0;
                    op.LostUnstaked = 0;
                    op.LostExternalStaked = 0;
                    op.LostExternalUnstaked = 0;

                    var updates = await Db.StakingUpdates
                        .Where(x => x.DoubleBakingOpId == op.Id)
                        .OrderByDescending(x => x.Id)
                        .ToListAsync();

                    await new StakingUpdateCommit(Proto).Revert(updates);

                    op.StakingUpdatesCount = null;
                }
            }

            if (block.Events.HasFlag(BlockEvents.DoubleEndorsingSlashing))
            {
                foreach (var op in await Db.DoubleEndorsingOps.Where(x => x.SlashedLevel == block.Level).ToListAsync())
                {
                    var accuser = Cache.Accounts.GetDelegate(op.AccuserId);
                    Db.TryAttach(accuser);
                    accuser.Balance -= op.Reward;
                    accuser.StakingBalance -= op.Reward;

                    var accuserCycle = await Cache.BakerCycles.GetOrDefaultAsync(block.Cycle, accuser.Id);
                    if (accuserCycle != null)
                    {
                        Db.TryAttach(accuserCycle);
                        accuserCycle.DoubleEndorsingRewards -= op.Reward;
                    }

                    var offenderCycle = await Cache.BakerCycles.GetOrDefaultAsync(block.Cycle, op.OffenderId);
                    if (offenderCycle != null)
                    {
                        Db.TryAttach(offenderCycle);
                        offenderCycle.DoubleEndorsingLostStaked -= op.LostStaked;
                        offenderCycle.DoubleEndorsingLostUnstaked -= op.LostUnstaked;
                        offenderCycle.DoubleEndorsingLostExternalStaked -= op.LostExternalStaked;
                        offenderCycle.DoubleEndorsingLostExternalUnstaked -= op.LostExternalUnstaked;
                    }

                    op.Reward = 0;
                    op.LostStaked = 0;
                    op.LostUnstaked = 0;
                    op.LostExternalStaked = 0;
                    op.LostExternalUnstaked = 0;

                    var updates = await Db.StakingUpdates
                        .Where(x => x.DoubleEndorsingOpId == op.Id)
                        .OrderByDescending(x => x.Id)
                        .ToListAsync();

                    await new StakingUpdateCommit(Proto).Revert(updates);

                    op.StakingUpdatesCount = null;
                }
            }

            if (block.Events.HasFlag(BlockEvents.DoublePreendorsingSlashing))
            {
                foreach (var op in await Db.DoublePreendorsingOps.Where(x => x.SlashedLevel == block.Level).ToListAsync())
                {
                    var accuser = Cache.Accounts.GetDelegate(op.AccuserId);
                    Db.TryAttach(accuser);
                    accuser.Balance -= op.Reward;
                    accuser.StakingBalance -= op.Reward;

                    var accuserCycle = await Cache.BakerCycles.GetOrDefaultAsync(block.Cycle, accuser.Id);
                    if (accuserCycle != null)
                    {
                        Db.TryAttach(accuserCycle);
                        accuserCycle.DoublePreendorsingRewards -= op.Reward;
                    }

                    var offenderCycle = await Cache.BakerCycles.GetOrDefaultAsync(block.Cycle, op.OffenderId);
                    if (offenderCycle != null)
                    {
                        Db.TryAttach(offenderCycle);
                        offenderCycle.DoublePreendorsingLostStaked -= op.LostStaked;
                        offenderCycle.DoublePreendorsingLostUnstaked -= op.LostUnstaked;
                        offenderCycle.DoublePreendorsingLostExternalStaked -= op.LostExternalStaked;
                        offenderCycle.DoublePreendorsingLostExternalUnstaked -= op.LostExternalUnstaked;
                    }

                    op.Reward = 0;
                    op.LostStaked = 0;
                    op.LostUnstaked = 0;
                    op.LostExternalStaked = 0;
                    op.LostExternalUnstaked = 0;

                    var updates = await Db.StakingUpdates
                        .Where(x => x.DoublePreendorsingOpId == op.Id)
                        .OrderByDescending(x => x.Id)
                        .ToListAsync();

                    await new StakingUpdateCommit(Proto).Revert(updates);

                    op.StakingUpdatesCount = null;
                }
            }
        }

        async Task<Dictionary<string, List<(int stakerId, int cycle, long slashed)>>> SlashUnstakeRequests(
            Block block,
            IEnumerable<JsonElement> slashings)
        {
            var slashedBakers = slashings
                .Where(x => x.RequiredString("kind") == "freezer")
                .Select(GetFreezerBaker)
                .ToHashSet()
                .Select(Cache.Accounts.GetExistingDelegate);

            var slashedRequests = new Dictionary<int, List<(int, int, long)>>();
            foreach (var baker in slashedBakers)
            {
                var bakerContext = await Proto.Rpc.GetContractRawAsync(block.Level, baker.Address);
                if (bakerContext.TryGetProperty("unstaked_frozen_deposits", out var prop))
                {
                    var cycles = prop.RequiredArray().EnumerateArray()
                        .Select(x => x.RequiredArray().EnumerateArray().First().RequiredInt32())
                        .ToHashSet();

                    var requests = await Db.UnstakeRequests
                        .Where(x => x.BakerId == baker.Id && cycles.Contains(x.Cycle) && x.StakerId != null)
                        .ToListAsync();

                    foreach (var request in requests)
                        Cache.UnstakeRequests.Add(request);

                    var stakers = requests
                        .Select(x => x.StakerId!.Value)
                        .ToHashSet();

                    var slashedBakerRequests = new List<(int, int, long)>(requests.Count);
                    foreach (var stakerId in stakers)
                    {
                        var staker = await Cache.Accounts.GetAsync(stakerId);
                        var rpc = await Proto.Rpc.GetUnstakeRequests(block.Level, staker.Address);
                        if (rpc.ValueKind != JsonValueKind.Null)
                        {
                            foreach (var request in rpc.Required("unfinalizable").RequiredArray("requests").EnumerateArray())
                            {
                                var cycle = request.RequiredInt32("cycle");
                                var actualAmount = request.RequiredInt64("amount");

                                var local = requests.First(x => x.StakerId == staker.Id && x.Cycle == cycle);
                                var localActualAmount = local.RequestedAmount - local.RestakedAmount - local.FinalizedAmount - local.SlashedAmount + (local.RoundingError ?? 0);

                                if (localActualAmount != actualAmount)
                                    slashedBakerRequests.Add((staker.Id, cycle, localActualAmount - actualAmount));
                            }
                        }
                    }

                    if (slashedBakerRequests.Any())
                        slashedRequests.Add(baker.Id, slashedBakerRequests);
                }
            }

            var bakerByOp = slashings
                .Where(x => x.RequiredString("kind") == "freezer")
                .DistinctBy(x => x.RequiredString("delayed_operation_hash"))
                .ToDictionary(x => x.RequiredString("delayed_operation_hash"), GetFreezerBaker);

            var burnedByOp = slashings
                .Where(x => x.RequiredString("kind") == "burned")
                .ToDictionary(x => x.RequiredString("delayed_operation_hash"), x => x.RequiredInt64("change"));

            var burnedByBaker = burnedByOp
                .GroupBy(x => bakerByOp[x.Key])
                .ToDictionary(x => x.Key, x => x.Sum(y => y.Value));

            var shares = burnedByOp
                .ToDictionary(x => x.Key, x => x.Value / (double)burnedByBaker[bakerByOp[x.Key]]);

            var opsByBaker = burnedByOp.Keys
                .GroupBy(x => Cache.Accounts.GetExistingDelegate(bakerByOp[x]).Id)
                .ToDictionary(x => x.Key, x => x.ToList());

            var res = shares.Keys.ToDictionary(x => x, x => new List<(int stakerId, int cycle, long slashed)>());
            foreach (var (bakerId, bakerRequests) in slashedRequests)
            {
                var ops = opsByBaker[bakerId];
                foreach (var (stakerId, cycle, totalSlashed) in bakerRequests)
                {
                    var rest = totalSlashed;
                    foreach (var op in ops)
                    {
                        var slashed = (long)Math.Floor(totalSlashed * shares[op]);
                        res[op].Add((stakerId, cycle, slashed));
                        rest -= slashed;
                    }
                    var last = res[ops[^1]][^1];
                    res[ops[^1]][^1] = (last.stakerId, last.cycle, last.slashed + rest);
                }
            }

            return res;
        }

        async Task<List<StakingUpdate>> ParseStakingUpdates(
            Block block,
            BaseOperation operation,
            List<(int stakerId, int cycle, long slashed)> unstakeRequests,
            IEnumerable<JsonElement> balanceUpdates)
        {
            var bakerAddress = balanceUpdates
                .Where(x => x.RequiredString("kind") == "freezer")
                .Select(GetFreezerBaker)
                .First();
            var baker = Cache.Accounts.GetExistingDelegate(bakerAddress);
            var res = new List<StakingUpdate>();

            var slashedOwn = balanceUpdates
                .Where(x => x.RequiredString("kind") == "freezer" && x.RequiredString("category") == "deposits" && IsOwnStake(x))
                .Sum(x => -x.RequiredInt64("change"));

            var slashedExternal = balanceUpdates
                .Where(x => x.RequiredString("kind") == "freezer" && x.RequiredString("category") == "deposits" && IsExternalStake(x))
                .Sum(x => -x.RequiredInt64("change"));

            var slashedUnstaked = balanceUpdates
                .Where(x => x.RequiredString("kind") == "freezer" && x.RequiredString("category") == "unstaked_deposits" && IsExternalStake(x))
                .GroupBy(x => x.RequiredInt32("cycle"))
                .ToDictionary(x => x.Key, x => x.Sum(u => -u.RequiredInt64("change")));

            #region diagnostics
            if (slashedOwn == 0)
                throw new Exception("Baker own stake wasn't slashed");

            var burnedUpdate = balanceUpdates.SingleOrDefault(x => x.RequiredString("kind") == "burned");
            var burned = burnedUpdate.ValueKind != JsonValueKind.Undefined
                ? burnedUpdate.RequiredInt64("change")
                : 0;

            var rewardUpdate = balanceUpdates.SingleOrDefault(x => x.RequiredString("kind") == "contract");
            var reward = rewardUpdate.ValueKind != JsonValueKind.Undefined
                ? rewardUpdate.RequiredInt64("change")
                : 0;

            var totalSlashed = slashedOwn + slashedExternal;
            if (slashedUnstaked.Count > 0)
                totalSlashed += slashedUnstaked.Sum(x => x.Value);

            if (totalSlashed != burned + reward)
                throw new Exception("Wrong slashing balance updates");

            if (balanceUpdates.Where(x => x.RequiredString("kind") == "freezer").Select(GetFreezerBaker).ToHashSet().Count != 1)
                throw new Exception("Wrong slashing balance updates");
            #endregion

            #region slash own stake
            if (slashedOwn > 0)
            {
                var stakingUpdate = new StakingUpdate
                {
                    Id = ++Cache.AppState.Get().StakingUpdatesCount,
                    Level = block.Level,
                    Cycle = block.Cycle,
                    BakerId = baker.Id,
                    StakerId = baker.Id,
                    Type = StakingUpdateType.SlashStaked,
                    Amount = slashedOwn
                };
                switch (operation)
                {
                    case DoubleBakingOperation: stakingUpdate.DoubleBakingOpId = operation.Id; break;
                    case DoubleEndorsingOperation: stakingUpdate.DoubleEndorsingOpId = operation.Id; break;
                    case DoublePreendorsingOperation: stakingUpdate.DoublePreendorsingOpId = operation.Id; break;
                    default: throw new InvalidOperationException();
                }
                res.Add(stakingUpdate);
            }
            #endregion

            #region slash external stake
            if (slashedExternal > 0)
            {
                var stakers = await Db.Users
                    .Where(x => x.DelegateId == baker.Id && x.StakedPseudotokens != null)
                    .OrderBy(x => x.Id)
                    .ToListAsync();

                var newExternalStake = baker.ExternalStakedBalance - slashedExternal;

                var updates = new List<StakingUpdate>(stakers.Count);
                foreach (var staker in stakers)
                {
                    Cache.Accounts.Add(staker);

                    var prevStake = staker.StakedPseudotokens != baker.IssuedPseudotokens
                        ? (long)((BigInteger)baker.ExternalStakedBalance * staker.StakedPseudotokens!.Value / baker.IssuedPseudotokens!.Value)
                        : baker.ExternalStakedBalance;

                    var newStake = staker.StakedPseudotokens != baker.IssuedPseudotokens
                        ? (long)((BigInteger)newExternalStake * staker.StakedPseudotokens!.Value / baker.IssuedPseudotokens!.Value)
                        : newExternalStake;

                    if (prevStake != newStake)
                    {
                        updates.Add(new StakingUpdate
                        {
                            Id = ++Cache.AppState.Get().StakingUpdatesCount,
                            Level = block.Level,
                            Cycle = block.Cycle,
                            BakerId = baker.Id,
                            StakerId = staker.Id,
                            Type = StakingUpdateType.SlashStaked,
                            Amount = prevStake - newStake
                        });
                    }
                }

                switch (operation)
                {
                    case DoubleBakingOperation:
                        foreach (var update in updates)
                            update.DoubleBakingOpId = operation.Id;
                        break;
                    case DoubleEndorsingOperation:
                        foreach (var update in updates)
                            update.DoubleEndorsingOpId = operation.Id;
                        break;
                    case DoublePreendorsingOperation:
                        foreach (var update in updates)
                            update.DoublePreendorsingOpId = operation.Id;
                        break;
                    default: throw new InvalidOperationException();
                }

                var actuallySlashed = updates.Sum(x => x.Amount);
                if (actuallySlashed != slashedExternal)
                    updates[^1].RoundingError = actuallySlashed - slashedExternal;

                res.AddRange(updates);
            }
            #endregion

            #region slash unstaked
            if (unstakeRequests.Count > 0)
            {
                var updates = new List<StakingUpdate>(unstakeRequests.Count);
                foreach (var (stakerId, cycle, amount) in unstakeRequests.OrderBy(x => x.cycle).ThenBy(x => x.stakerId))
                {
                    updates.Add(new StakingUpdate
                    {
                        Id = ++Cache.AppState.Get().StakingUpdatesCount,
                        Level = block.Level,
                        Cycle = cycle,
                        BakerId = baker.Id,
                        StakerId = stakerId,
                        Type = StakingUpdateType.SlashUnstaked,
                        Amount = amount
                    });
                }

                switch (operation)
                {
                    case DoubleBakingOperation:
                        foreach (var update in updates)
                            update.DoubleBakingOpId = operation.Id;
                        break;
                    case DoubleEndorsingOperation:
                        foreach (var update in updates)
                            update.DoubleEndorsingOpId = operation.Id;
                        break;
                    case DoublePreendorsingOperation:
                        foreach (var update in updates)
                            update.DoublePreendorsingOpId = operation.Id;
                        break;
                    default: throw new InvalidOperationException();
                }

                foreach (var cycle in unstakeRequests.Select(x => x.cycle).ToHashSet())
                {
                    var slashed = slashedUnstaked.TryGetValue(cycle, out var v) ? v : 0;
                    var actuallySlashed = unstakeRequests.Where(x => x.cycle == cycle).Sum(x => x.slashed);
                    if (actuallySlashed != slashed)
                        updates.Last(x => x.Cycle == cycle).RoundingError = actuallySlashed - slashed;
                }

                res.AddRange(updates);
            }
            #endregion

            return res;
        }

        protected virtual string GetFreezerBaker(JsonElement update)
        {
            return update.Required("staker").OptionalString("baker")
                ?? update.Required("staker").RequiredString("delegate");
        }

        protected virtual bool IsOwnStake(JsonElement update)
        {
            return update.Required("staker").TryGetProperty("baker", out _);
        }

        protected virtual bool IsExternalStake(JsonElement update)
        {
            return update.Required("staker").TryGetProperty("delegate", out _);
        }
    }
}
