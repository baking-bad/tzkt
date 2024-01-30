using System.Numerics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto18
{
    class SlashingCommit : ProtocolCommit
    {
        public SlashingCommit(ProtocolHandler protocol) : base(protocol) { }

        public async Task Apply(Block block, JsonElement rawBlock)
        {
            if (!block.Events.HasFlag(BlockEvents.CycleEnd))
                return;

            var slashings = rawBlock
                .Required("metadata")
                .RequiredArray("balance_updates")
                .EnumerateArray()
                .Where(x => x.RequiredString("origin") == "delayed_operation")
                .GroupBy(x => x.RequiredString("delayed_operation_hash"))
                .Reverse(); // slashings are reversed in block balance updates for some reason ¯\_(ツ)_/¯

            foreach (var slashing in slashings)
            {
                var opHash = slashing.Key;
                var accusation = block.DoubleBakings?.FirstOrDefault(x => x.OpHash == opHash)
                    ?? block.DoubleEndorsings?.FirstOrDefault(x => x.OpHash == opHash)
                    ?? block.DoublePreendorsings?.FirstOrDefault(x => x.OpHash == opHash)
                    ?? Db.DoubleBakingOps.FirstOrDefault(x => x.OpHash == opHash)
                    ?? Db.DoubleEndorsingOps.FirstOrDefault(x => x.OpHash == opHash)
                    ?? (BaseOperation)Db.DoublePreendorsingOps.FirstOrDefault(x => x.OpHash == opHash)
                    ?? throw new Exception($"Cannot find delayed operation '{opHash}'");
                
                var (accusedLevel, accuserId, offenderId) = accusation switch
                {
                    DoubleBakingOperation op => (op.AccusedLevel, op.AccuserId, op.OffenderId),
                    DoubleEndorsingOperation op => (op.AccusedLevel, op.AccuserId, op.OffenderId),
                    DoublePreendorsingOperation op => (op.AccusedLevel, op.AccuserId, op.OffenderId),
                    _ => throw new InvalidOperationException()
                };
                var accuser = Cache.Accounts.GetDelegate(accuserId);
                var offender = Cache.Accounts.GetDelegate(offenderId);
                
                var contractUpdates = slashing.Where(x => x.RequiredString("kind") == "contract");
                var reward = contractUpdates.Any()
                    ? contractUpdates.Sum(x => x.RequiredInt64("change"))
                    : 0;

                Db.TryAttach(accuser);
                accuser.Balance += reward;
                accuser.StakingBalance += reward;

                var depositsUpdates = slashing.Where(x => x.RequiredString("kind") == "freezer" && x.RequiredString("category") == "deposits");
                var lostStaked = depositsUpdates.Any()
                    ? depositsUpdates.Sum(x => -x.RequiredInt64("change"))
                    : 0;
                var lostOwnStaked = offender.TotalStakedBalance == 0 ? lostStaked : (long)((BigInteger)lostStaked * offender.StakedBalance / offender.TotalStakedBalance);
                var lostExternalStaked = lostStaked - lostOwnStaked;

                var unstakedDepositsUpdates = slashing.Where(x => x.RequiredString("kind") == "freezer" && x.RequiredString("category") == "unstaked_deposits");
                var lostUnstaked = unstakedDepositsUpdates.Any()
                    ? unstakedDepositsUpdates.Sum(x => -x.RequiredInt64("change"))
                    : 0;
                var lostOwnUnstaked = offender.TotalStakedBalance == 0 ? lostUnstaked : (long)((BigInteger)lostUnstaked * offender.StakedBalance / offender.TotalStakedBalance);
                var lostExternalUnstaked = lostUnstaked - lostOwnUnstaked;

                var preservedCycle = Math.Max(0, block.Cycle - block.Protocol.PreservedCycles);
                var accusedCycle = await Cache.Protocols.GetCycle(accusedLevel);
                var autostakingOps = await Db.AutostakingOps
                    .AsNoTracking()
                    .Where(x => x.BakerId == offender.Id &&
                                x.Cycle >= preservedCycle &&
                                x.Cycle <= accusedCycle)
                    .ToListAsync();

                var unstakeRequests = new Dictionary<int, long>();
                foreach (var op in autostakingOps)
                {
                    if (op.Action == AutostakingAction.Unstake)
                        unstakeRequests.Add(op.Cycle, op.Amount);
                    else if (op.Action == AutostakingAction.Restake)
                        if (unstakeRequests.ContainsKey(op.Cycle))
                            unstakeRequests[op.Cycle] -= op.Amount;
                }

                var roundingLoss = 0L;
                foreach (var (cycle, deposits) in unstakeRequests)
                {
                    var cycleStart = (await Cache.Protocols.FindByCycleAsync(cycle)).GetCycleStart(cycle);
                    var unstakedDeposits = deposits;

                    var prevSlashings = (await Db.DoubleBakingOps
                        .AsNoTracking()
                        .Where(x => x.OffenderId == offender.Id && x.AccusedLevel >= cycleStart && x.Id < accusation.Id)
                        .Select(x => new { x.Id, x.Level, Type = 0 })
                        .ToListAsync())
                        .Concat(await Db.DoubleEndorsingOps
                        .AsNoTracking()
                        .Where(x => x.OffenderId == offender.Id && x.AccusedLevel >= cycleStart && x.Id < accusation.Id)
                        .Select(x => new { x.Id, x.Level, Type = 1 })
                        .ToListAsync())
                        .Concat(await Db.DoublePreendorsingOps
                        .AsNoTracking()
                        .Where(x => x.OffenderId == offender.Id && x.AccusedLevel >= cycleStart && x.Id < accusation.Id)
                        .Select(x => new { x.Id, x.Level, Type = 2 })
                        .ToListAsync())
                        .OrderBy(x => x.Id);

                    foreach (var prevSlashing in prevSlashings)
                    {
                        var protocol = await Cache.Protocols.FindByLevelAsync(prevSlashing.Level);
                        var percentage = prevSlashing.Type switch
                        {
                            0 => protocol.DoubleBakingSlashedPercentage,
                            _ => protocol.DoubleEndorsingSlashedPercentage
                        };
                        unstakedDeposits -= (deposits * percentage + 99) / 100;
                    }

                    if (unstakedDeposits > 0)
                    {
                        var slashedPercentage = accusation switch
                        {
                            DoubleBakingOperation => block.Protocol.DoubleBakingSlashedPercentage,
                            _ => block.Protocol.DoubleEndorsingSlashedPercentage
                        };
                        roundingLoss += (deposits * slashedPercentage + 99) / 100 - deposits * slashedPercentage / 100;
                    }
                }

                Db.TryAttach(offender);
                offender.Balance -= lostOwnStaked + lostOwnUnstaked;
                offender.StakingBalance -= lostOwnStaked + lostOwnUnstaked + lostExternalStaked + lostExternalUnstaked;
                offender.StakedBalance -= lostOwnStaked;
                offender.UnstakedBalance -= lostOwnUnstaked;
                offender.ExternalStakedBalance -= lostExternalStaked;
                offender.ExternalUnstakedBalance -= lostExternalUnstaked;
                offender.TotalStakedBalance -= lostOwnStaked + lostExternalStaked;
                offender.DelegatedBalance -= lostExternalUnstaked;
                offender.LostBalance += roundingLoss;

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
                        op.RoundingLoss = roundingLoss;
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
                        op.RoundingLoss = roundingLoss;
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
                        op.RoundingLoss = roundingLoss;
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

                var stats = Cache.Statistics.Current;
                Db.TryAttach(stats);
                stats.TotalBurned += lostOwnStaked + lostExternalStaked + lostOwnUnstaked + lostExternalUnstaked - reward;
                stats.TotalFrozen -= lostOwnStaked + lostExternalStaked;
                stats.TotalLost += roundingLoss;
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

                    var offender = Cache.Accounts.GetDelegate(op.OffenderId);
                    Db.TryAttach(offender);
                    offender.Balance += op.LostStaked + op.LostUnstaked;
                    offender.StakingBalance += op.LostStaked + op.LostUnstaked + op.LostExternalStaked + op.LostExternalUnstaked;
                    offender.StakedBalance += op.LostStaked;
                    offender.UnstakedBalance += op.LostUnstaked;
                    offender.ExternalStakedBalance += op.LostExternalStaked;
                    offender.ExternalUnstakedBalance += op.LostExternalUnstaked;
                    offender.TotalStakedBalance += op.LostStaked + op.LostExternalStaked;
                    offender.DelegatedBalance += op.LostExternalUnstaked;
                    offender.LostBalance -= op.RoundingLoss;

                    var accuserCycle = await Cache.BakerCycles.GetOrDefaultAsync(block.Cycle, accuser.Id);
                    if (accuserCycle != null)
                    {
                        Db.TryAttach(accuserCycle);
                        accuserCycle.DoubleBakingRewards -= op.Reward;
                    }

                    var offenderCycle = await Cache.BakerCycles.GetOrDefaultAsync(block.Cycle, offender.Id);
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
                    op.RoundingLoss = 0;
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

                    var offender = Cache.Accounts.GetDelegate(op.OffenderId);
                    Db.TryAttach(offender);
                    offender.Balance += op.LostStaked + op.LostUnstaked;
                    offender.StakingBalance += op.LostStaked + op.LostUnstaked + op.LostExternalStaked + op.LostExternalUnstaked;
                    offender.StakedBalance += op.LostStaked;
                    offender.UnstakedBalance += op.LostUnstaked;
                    offender.ExternalStakedBalance += op.LostExternalStaked;
                    offender.ExternalUnstakedBalance += op.LostExternalUnstaked;
                    offender.TotalStakedBalance += op.LostStaked + op.LostExternalStaked;
                    offender.DelegatedBalance += op.LostExternalUnstaked;
                    offender.LostBalance -= op.RoundingLoss;

                    var accuserCycle = await Cache.BakerCycles.GetOrDefaultAsync(block.Cycle, accuser.Id);
                    if (accuserCycle != null)
                    {
                        Db.TryAttach(accuserCycle);
                        accuserCycle.DoubleEndorsingRewards -= op.Reward;
                    }

                    var offenderCycle = await Cache.BakerCycles.GetOrDefaultAsync(block.Cycle, offender.Id);
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
                    op.RoundingLoss = 0;
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

                    var offender = Cache.Accounts.GetDelegate(op.OffenderId);
                    Db.TryAttach(offender);
                    offender.Balance += op.LostStaked + op.LostUnstaked;
                    offender.StakingBalance += op.LostStaked + op.LostUnstaked + op.LostExternalStaked + op.LostExternalUnstaked;
                    offender.StakedBalance += op.LostStaked;
                    offender.UnstakedBalance += op.LostUnstaked;
                    offender.ExternalStakedBalance += op.LostExternalStaked;
                    offender.ExternalUnstakedBalance += op.LostExternalUnstaked;
                    offender.TotalStakedBalance += op.LostStaked + op.LostExternalStaked;
                    offender.DelegatedBalance += op.LostExternalUnstaked;
                    offender.LostBalance -= op.RoundingLoss;

                    var accuserCycle = await Cache.BakerCycles.GetOrDefaultAsync(block.Cycle, accuser.Id);
                    if (accuserCycle != null)
                    {
                        Db.TryAttach(accuserCycle);
                        accuserCycle.DoublePreendorsingRewards -= op.Reward;
                    }

                    var offenderCycle = await Cache.BakerCycles.GetOrDefaultAsync(block.Cycle, offender.Id);
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
                    op.RoundingLoss = 0;
                }
            }
        }
    }
}
