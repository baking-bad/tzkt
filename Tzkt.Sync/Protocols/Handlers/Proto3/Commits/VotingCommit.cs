using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto3
{
    class VotingCommit : ProtocolCommit
    {
        public VotingCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual async Task Apply(Block block, JsonElement rawBlock)
        {
            #region init
            var events = BlockEvents.None;
            VotingPeriod period = null;
            List<VotingSnapshot> rolls = null;

            if (block.Events.HasFlag(BlockEvents.VotingPeriodEnd))
            {
                events = BlockEvents.VotingPeriodEnd;
                period = await Cache.Periods.CurrentAsync();
                period.Epoch ??= await Db.VotingEpoches.FirstOrDefaultAsync(x => x.Id == period.EpochId);
            }
            else if (block.Events.HasFlag(BlockEvents.VotingPeriodBegin))
            {
                events = BlockEvents.VotingPeriodBegin;
                var currentPeriod = await Cache.Periods.CurrentAsync();
                var currentEpoch = await Db.VotingEpoches.FirstOrDefaultAsync(x => x.Id == currentPeriod.EpochId);

                var periodKind = rawBlock.Required("metadata").RequiredString("voting_period_kind");
                if (periodKind == "proposal")
                {
                    #region start proposal period
                    period = new ProposalPeriod
                    {
                        Code = currentPeriod.Code + 1,
                        Epoch = new VotingEpoch { Level = block.Level },
                        Kind = VotingPeriods.Proposal,
                        StartLevel = block.Level,
                        EndLevel = block.Level + block.Protocol.BlocksPerVoting - 1
                    };
                    #endregion
                }
                else if (periodKind == "testing_vote")
                {
                    #region start exploration period
                    var proposal = await Db.Proposals
                        .Where(x => x.ProposalPeriodId == currentPeriod.Id)
                        .OrderByDescending(x => x.Upvotes)
                        .FirstAsync();

                    Cache.Proposals.Add(proposal);

                    period = new ExplorationPeriod
                    {
                        Code = currentPeriod.Code + 1,
                        Epoch = currentEpoch,
                        Kind = VotingPeriods.Exploration,
                        StartLevel = block.Level,
                        EndLevel = block.Level + block.Protocol.BlocksPerVoting - 1,
                        Proposal = proposal,
                        ProposalId = proposal.Id
                    };
                    #endregion
                }
                else if (periodKind == "testing")
                {
                    #region start testing period
                    period = new TestingPeriod
                    {
                        Code = currentPeriod.Code + 1,
                        Epoch = currentEpoch,
                        Kind = VotingPeriods.Testing,
                        StartLevel = block.Level,
                        EndLevel = block.Level + block.Protocol.BlocksPerVoting - 1,
                        Proposal = await Cache.Proposals.GetAsync((currentPeriod as ExplorationPeriod).ProposalId),
                        ProposalId = (currentPeriod as ExplorationPeriod).ProposalId
                    };
                    #endregion
                }
                else if (periodKind == "promotion_vote")
                {
                    #region start promotion period
                    period = new PromotionPeriod
                    {
                        Code = currentPeriod.Code + 1,
                        Epoch = currentEpoch,
                        Kind = VotingPeriods.Promotion,
                        StartLevel = block.Level,
                        EndLevel = block.Level + block.Protocol.BlocksPerVoting - 1,
                        Proposal = await Cache.Proposals.GetAsync((currentPeriod as TestingPeriod).ProposalId),
                        ProposalId = (currentPeriod as TestingPeriod).ProposalId
                    };
                    #endregion
                }
                else
                {
                    throw new Exception("invalid voting period");
                }

                if (!(period is TestingPeriod))
                {
                    var gracePeriod = GracePeriod.Init(block); // TODO: fix crutch
                    var delegates = await Db.Delegates
                        .AsNoTracking()
                        .Where(x => x.Staked && x.DeactivationLevel < gracePeriod && x.StakingBalance >= block.Protocol.TokensPerRoll)
                        .ToListAsync();

                    var lastBlock = await Cache.Blocks.CurrentAsync();
                    lastBlock.Protocol ??= await Cache.Protocols.GetAsync(lastBlock.ProtoCode);

                    rolls = new List<VotingSnapshot>(delegates.Count);
                    foreach (var delegat in delegates)
                    {
                        rolls.Add(new VotingSnapshot
                        {
                            Level = lastBlock.Level,
                            Period = period,
                            DelegateId = delegat.Id,
                            Rolls = (int)(delegat.StakingBalance / lastBlock.Protocol.TokensPerRoll)
                        });
                    }

                    if (period is ExplorationPeriod exploration)
                        exploration.TotalStake = rolls.Sum(x => x.Rolls);
                    else if (period is PromotionPeriod promotion)
                        promotion.TotalStake = rolls.Sum(x => x.Rolls);
                }
            }
            #endregion

            if (events == BlockEvents.VotingPeriodEnd)
            {
                #region entities
                var epoch = period.Epoch;

                Db.TryAttach(epoch);
                #endregion

                epoch.Progress++;
            }
            else if (events == BlockEvents.VotingPeriodBegin)
            {
                #region entities
                if (period is ExplorationPeriod exploration)
                    Db.TryAttach(exploration.Proposal);
                else if (period is TestingPeriod testing)
                    Db.TryAttach(testing.Proposal);
                else if (period is PromotionPeriod promotion)
                    Db.TryAttach(promotion.Proposal);
                #endregion

                Db.VotingPeriods.Add(period);
                Cache.Periods.Add(period);

                if (rolls != null)
                    Db.VotingSnapshots.AddRange(rolls);
            }
        }

        public virtual async Task Revert(Block block)
        {
            #region init
            var events = BlockEvents.None;
            VotingPeriod period = null;
            List<VotingSnapshot> rolls = null;

            if (block.Events.HasFlag(BlockEvents.VotingPeriodEnd))
            {
                events = BlockEvents.VotingPeriodEnd;
                period = await Cache.Periods.CurrentAsync();
                period.Epoch ??= await Db.VotingEpoches.FirstOrDefaultAsync(x => x.Id == period.EpochId);
            }
            else if (block.Events.HasFlag(BlockEvents.VotingPeriodBegin))
            {
                events = BlockEvents.VotingPeriodBegin;
                period = await Cache.Periods.CurrentAsync();
                period.Epoch ??= await Db.VotingEpoches.FirstOrDefaultAsync(x => x.Id == period.EpochId);
                if (period is ExplorationPeriod exploration)
                    exploration.Proposal ??= await Cache.Proposals.GetAsync(exploration.ProposalId);
                if (period is TestingPeriod testing)
                    testing.Proposal ??= await Cache.Proposals.GetAsync(testing.ProposalId);
                else if (period is PromotionPeriod promotion)
                    promotion.Proposal ??= await Cache.Proposals.GetAsync(promotion.ProposalId);

                if (!(period is TestingPeriod))
                    rolls = await Db.VotingSnapshots.Where(x => x.Level == block.Level - 1).ToListAsync();
            }
            #endregion

            if (events == BlockEvents.VotingPeriodEnd)
            {
                #region entities
                var epoch = period.Epoch;

                Db.TryAttach(epoch);
                #endregion

                epoch.Progress--;
            }
            else if (events == BlockEvents.VotingPeriodBegin)
            {
                #region entities
                if (period is ExplorationPeriod exploration)
                    Db.TryAttach(exploration.Proposal);
                else if (period is TestingPeriod testing)
                    Db.TryAttach(testing.Proposal);
                else if (period is PromotionPeriod promotion)
                    Db.TryAttach(promotion.Proposal);
                #endregion

                if (period.Epoch.Progress == 0)
                    Db.VotingEpoches.Remove(period.Epoch);

                Db.VotingPeriods.Remove(period);
                Cache.Periods.Remove();

                if (rolls != null)
                    Db.VotingSnapshots.RemoveRange(rolls);
            }
        }

        public override Task Apply() => Task.CompletedTask;
        public override Task Revert() => Task.CompletedTask;
    }
}
