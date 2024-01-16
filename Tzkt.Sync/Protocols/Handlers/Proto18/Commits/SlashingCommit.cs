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
                .GroupBy(x => x.RequiredString("delayed_operation_hash"));

            foreach (var slashing in slashings)
            {
                if (slashing.Any(x => x.RequiredString("kind") == "freezer" && x.RequiredString("category") == "unstaked_deposits"))
                    // there are also ("freezer", "unstaked_deposits") updates, but they don't work properly in oxford,
                    // so we count slashed unstaked deposits at the moment of finalize_update
                    // TODO: count slashing here, when all protocol bugs are fixed
                    throw new NotImplementedException();

                var freezerUpdates = slashing.Where(x => x.RequiredString("kind") == "freezer" && x.RequiredString("category") == "deposits");
                var contractUpdates = slashing.Where(x => x.RequiredString("kind") == "contract");

                var accuserAddr = contractUpdates.Any()
                    ? contractUpdates.First().RequiredString("contract")
                    : block.Proposer.Address; // this is wrong, but no big deal
                var accuser = Cache.Accounts.GetDelegate(accuserAddr);
                var accuserReward = contractUpdates.Any()
                    ? contractUpdates.Sum(x => x.RequiredInt64("change"))
                    : 0;

                Db.TryAttach(accuser);
                accuser.Balance += accuserReward;
                accuser.StakingBalance += accuserReward;

                var offenderAddr = freezerUpdates.Any()
                    ? freezerUpdates.First().Required("staker").RequiredString("baker")
                    : block.Proposer.Address; // this is wrong, but no big deal
                var offender = Cache.Accounts.GetDelegate(offenderAddr);
                var offenderLoss = freezerUpdates.Any()
                    ? freezerUpdates.Sum(x => -x.RequiredInt64("change"))
                    : 0;
                var offenderLossOwn = offender.TotalStakedBalance == 0 ? offenderLoss : (long)((BigInteger)offenderLoss * offender.StakedBalance / offender.TotalStakedBalance);
                var offenderLossShared = offenderLoss - offenderLossOwn;

                Db.TryAttach(offender);
                offender.Balance -= offenderLossOwn;
                offender.StakingBalance -= offenderLossOwn + offenderLossShared;
                offender.StakedBalance -= offenderLossOwn;
                offender.ExternalStakedBalance -= offenderLossShared;
                offender.TotalStakedBalance -= offenderLossOwn + offenderLossShared;

                var opHash = slashing.Key;
                var accusation = block.DoubleBakings?.FirstOrDefault(x => x.OpHash == opHash)
                    ?? block.DoubleEndorsings?.FirstOrDefault(x => x.OpHash == opHash)
                    ?? block.DoublePreendorsings?.FirstOrDefault(x => x.OpHash == opHash)
                    ?? Db.DoubleBakingOps.FirstOrDefault(x => x.OpHash == opHash)
                    ?? Db.DoubleEndorsingOps.FirstOrDefault(x => x.OpHash == opHash)
                    ?? (BaseOperation)Db.DoublePreendorsingOps.FirstOrDefault(x => x.OpHash == opHash)
                    ?? throw new Exception($"Cannot find delayed operation '{opHash}'");
                var accuserCycle = await Cache.BakerCycles.GetOrDefaultAsync(block.Cycle, accuser.Id);
                var offenderCycle = await Cache.BakerCycles.GetOrDefaultAsync(block.Cycle, offender.Id);

                switch (accusation)
                {
                    case DoubleBakingOperation op:
                        #region temp check
                        if (op.SlashedLevel != block.Level ||
                            op.AccuserId != accuser.Id && accuserReward > 0 ||
                            op.OffenderId != offender.Id && offenderLoss > 0)
                            throw new Exception("Unexpected slashing conditions");
                        #endregion
                        op.AccuserReward = accuserReward;
                        op.OffenderLossOwn = offenderLossOwn;
                        op.OffenderLossShared = offenderLossShared;
                        if (accuserCycle != null)
                        {
                            Db.TryAttach(accuserCycle);
                            accuserCycle.DoubleBakingRewards += accuserReward;
                        }
                        if (offenderCycle != null)
                        {
                            Db.TryAttach(offenderCycle);
                            offenderCycle.DoubleBakingLossesOwn += offenderLossOwn;
                            offenderCycle.DoubleBakingLossesShared += offenderLossShared;
                        }
                        block.Events |= BlockEvents.DoubleBakingSlashing;
                        break;
                    case DoubleEndorsingOperation op:
                        #region temp check
                        if (op.SlashedLevel != block.Level ||
                            op.AccuserId != accuser.Id && accuserReward > 0 ||
                            op.OffenderId != offender.Id && offenderLoss > 0)
                            throw new Exception("Unexpected slashing conditions");
                        #endregion
                        op.AccuserReward = accuserReward;
                        op.OffenderLossOwn = offenderLossOwn;
                        op.OffenderLossShared = offenderLossShared;
                        if (accuserCycle != null)
                        {
                            Db.TryAttach(accuserCycle);
                            accuserCycle.DoubleEndorsingRewards += accuserReward;
                        }
                        if (offenderCycle != null)
                        {
                            Db.TryAttach(offenderCycle);
                            offenderCycle.DoubleEndorsingLossesOwn += offenderLossOwn;
                            offenderCycle.DoubleEndorsingLossesShared += offenderLossShared;
                        }
                        block.Events |= BlockEvents.DoubleEndorsingSlashing;
                        break;
                    case DoublePreendorsingOperation op:
                        #region temp check
                        if (op.SlashedLevel != block.Level ||
                            op.AccuserId != accuser.Id && accuserReward > 0 ||
                            op.OffenderId != offender.Id && offenderLoss > 0)
                            throw new Exception("Unexpected slashing conditions");
                        #endregion
                        op.AccuserReward = accuserReward;
                        op.OffenderLossOwn = offenderLossOwn;
                        op.OffenderLossShared = offenderLossShared;
                        if (accuserCycle != null)
                        {
                            Db.TryAttach(accuserCycle);
                            accuserCycle.DoublePreendorsingRewards += accuserReward;
                        }
                        if (offenderCycle != null)
                        {
                            Db.TryAttach(offenderCycle);
                            offenderCycle.DoublePreendorsingLossesOwn += offenderLossOwn;
                            offenderCycle.DoublePreendorsingLossesShared += offenderLossShared;
                        }
                        block.Events |= BlockEvents.DoublePreendorsingSlashing;
                        break;
                    default:
                        throw new InvalidOperationException();
                }

                Cache.Statistics.Current.TotalBurned += offenderLossOwn + offenderLossShared - accuserReward;
                Cache.Statistics.Current.TotalFrozen -= offenderLossOwn + offenderLossShared;
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
                    accuser.Balance -= op.AccuserReward;
                    accuser.StakingBalance -= op.AccuserReward;

                    var offender = Cache.Accounts.GetDelegate(op.OffenderId);
                    Db.TryAttach(offender);
                    offender.Balance += op.OffenderLossOwn;
                    offender.StakingBalance += op.OffenderLossOwn + op.OffenderLossShared;
                    offender.StakedBalance += op.OffenderLossOwn;
                    offender.ExternalStakedBalance += op.OffenderLossShared;
                    offender.TotalStakedBalance += op.OffenderLossOwn + op.OffenderLossShared;

                    var accuserCycle = await Cache.BakerCycles.GetOrDefaultAsync(block.Cycle, accuser.Id);
                    if (accuserCycle != null)
                    {
                        Db.TryAttach(accuserCycle);
                        accuserCycle.DoubleBakingRewards -= op.AccuserReward;
                    }

                    var offenderCycle = await Cache.BakerCycles.GetOrDefaultAsync(block.Cycle, offender.Id);
                    if (offenderCycle != null)
                    {
                        Db.TryAttach(offenderCycle);
                        offenderCycle.DoubleBakingLossesOwn -= op.OffenderLossOwn;
                        offenderCycle.DoubleBakingLossesShared -= op.OffenderLossShared;
                    }

                    op.AccuserReward = 0;
                    op.OffenderLossOwn = 0;
                    op.OffenderLossShared = 0;

                    Cache.Statistics.Current.TotalBurned -= op.OffenderLossOwn + op.OffenderLossShared - op.AccuserReward;
                    Cache.Statistics.Current.TotalFrozen += op.OffenderLossOwn + op.OffenderLossShared;
                }
            }

            if (block.Events.HasFlag(BlockEvents.DoubleEndorsingSlashing))
            {
                foreach (var op in await Db.DoubleEndorsingOps.Where(x => x.SlashedLevel == block.Level).ToListAsync())
                {
                    var accuser = Cache.Accounts.GetDelegate(op.AccuserId);
                    Db.TryAttach(accuser);
                    accuser.Balance -= op.AccuserReward;
                    accuser.StakingBalance -= op.AccuserReward;

                    var offender = Cache.Accounts.GetDelegate(op.OffenderId);
                    Db.TryAttach(offender);
                    offender.Balance += op.OffenderLossOwn;
                    offender.StakingBalance += op.OffenderLossOwn + op.OffenderLossShared;
                    offender.StakedBalance += op.OffenderLossOwn;
                    offender.ExternalStakedBalance += op.OffenderLossShared;
                    offender.TotalStakedBalance += op.OffenderLossOwn + op.OffenderLossShared;

                    var accuserCycle = await Cache.BakerCycles.GetOrDefaultAsync(block.Cycle, accuser.Id);
                    if (accuserCycle != null)
                    {
                        Db.TryAttach(accuserCycle);
                        accuserCycle.DoubleEndorsingRewards -= op.AccuserReward;
                    }

                    var offenderCycle = await Cache.BakerCycles.GetOrDefaultAsync(block.Cycle, offender.Id);
                    if (offenderCycle != null)
                    {
                        Db.TryAttach(offenderCycle);
                        offenderCycle.DoubleEndorsingLossesOwn -= op.OffenderLossOwn;
                        offenderCycle.DoubleEndorsingLossesShared -= op.OffenderLossShared;
                    }

                    op.AccuserReward = 0;
                    op.OffenderLossOwn = 0;
                    op.OffenderLossShared = 0;

                    Cache.Statistics.Current.TotalBurned -= op.OffenderLossOwn + op.OffenderLossShared - op.AccuserReward;
                    Cache.Statistics.Current.TotalFrozen += op.OffenderLossOwn + op.OffenderLossShared;
                }
            }

            if (block.Events.HasFlag(BlockEvents.DoublePreendorsingSlashing))
            {
                foreach (var op in await Db.DoublePreendorsingOps.Where(x => x.SlashedLevel == block.Level).ToListAsync())
                {
                    var accuser = Cache.Accounts.GetDelegate(op.AccuserId);
                    Db.TryAttach(accuser);
                    accuser.Balance -= op.AccuserReward;
                    accuser.StakingBalance -= op.AccuserReward;

                    var offender = Cache.Accounts.GetDelegate(op.OffenderId);
                    Db.TryAttach(offender);
                    offender.Balance += op.OffenderLossOwn;
                    offender.StakingBalance += op.OffenderLossOwn + op.OffenderLossShared;
                    offender.StakedBalance += op.OffenderLossOwn;
                    offender.ExternalStakedBalance += op.OffenderLossShared;
                    offender.TotalStakedBalance += op.OffenderLossOwn + op.OffenderLossShared;

                    var accuserCycle = await Cache.BakerCycles.GetOrDefaultAsync(block.Cycle, accuser.Id);
                    if (accuserCycle != null)
                    {
                        Db.TryAttach(accuserCycle);
                        accuserCycle.DoublePreendorsingRewards -= op.AccuserReward;
                    }

                    var offenderCycle = await Cache.BakerCycles.GetOrDefaultAsync(block.Cycle, offender.Id);
                    if (offenderCycle != null)
                    {
                        Db.TryAttach(offenderCycle);
                        offenderCycle.DoublePreendorsingLossesOwn -= op.OffenderLossOwn;
                        offenderCycle.DoublePreendorsingLossesShared -= op.OffenderLossShared;
                    }

                    op.AccuserReward = 0;
                    op.OffenderLossOwn = 0;
                    op.OffenderLossShared = 0;

                    Cache.Statistics.Current.TotalBurned -= op.OffenderLossOwn + op.OffenderLossShared - op.AccuserReward;
                    Cache.Statistics.Current.TotalFrozen += op.OffenderLossOwn + op.OffenderLossShared;
                }
            }
        }
    }
}
