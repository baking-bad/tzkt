using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto6
{
    class VotingCommit : ProtocolCommit
    {
        public BlockEvents Event { get; private set; }
        public VotingPeriod Period { get; private set; }
        public List<VotingSnapshot> Rolls { get; private set; }

        VotingCommit(ProtocolHandler protocol) : base(protocol) { }

        public async Task Init(Block block, RawBlock rawBlock)
        {
            if (block.Events.HasFlag(BlockEvents.VotingPeriodEnd))
            {
                Event = BlockEvents.VotingPeriodEnd;
                Period = await Cache.Periods.CurrentAsync();
                Period.Epoch ??= await Db.VotingEpoches.FirstOrDefaultAsync(x => x.Id == Period.EpochId);
            }
            else if (block.Events.HasFlag(BlockEvents.VotingPeriodBegin))
            {
                Event = BlockEvents.VotingPeriodBegin;
                var protocol = await Cache.Protocols.GetAsync(rawBlock.Protocol);

                var currentPeriod = await Cache.Periods.CurrentAsync();
                var currentEpoch = await Db.VotingEpoches.FirstOrDefaultAsync(x => x.Id == currentPeriod.EpochId);

                if (rawBlock.Metadata.VotingPeriod == "proposal")
                {
                    #region start proposal period
                    Period = new ProposalPeriod
                    {
                        Code = currentPeriod.Code + 1,
                        Epoch = new VotingEpoch { Level = rawBlock.Level },
                        Kind = VotingPeriods.Proposal,
                        StartLevel = rawBlock.Level,
                        EndLevel = rawBlock.Level + protocol.BlocksPerVoting - 1
                    };
                    #endregion
                }
                else if (rawBlock.Metadata.VotingPeriod == "testing_vote")
                {
                    #region start exploration period
                    var proposal = await Db.Proposals
                        .Where(x => x.ProposalPeriodId == currentPeriod.Id)
                        .OrderByDescending(x => x.Upvotes)
                        .FirstAsync();

                    Cache.Proposals.Add(proposal);

                    Period = new ExplorationPeriod
                    {
                        Code = currentPeriod.Code + 1,
                        Epoch = currentEpoch,
                        Kind = VotingPeriods.Exploration,
                        StartLevel = rawBlock.Level,
                        EndLevel = rawBlock.Level + protocol.BlocksPerVoting - 1,
                        Proposal = proposal,
                        ProposalId = proposal.Id
                    };
                    #endregion
                }
                else if (rawBlock.Metadata.VotingPeriod == "testing")
                {
                    #region start testing period
                    Period = new TestingPeriod
                    {
                        Code = currentPeriod.Code + 1,
                        Epoch = currentEpoch,
                        Kind = VotingPeriods.Testing,
                        StartLevel = rawBlock.Level,
                        EndLevel = rawBlock.Level + protocol.BlocksPerVoting - 1,
                        Proposal = await Cache.Proposals.GetAsync((currentPeriod as ExplorationPeriod).ProposalId),
                        ProposalId = (currentPeriod as ExplorationPeriod).ProposalId
                    };
                    #endregion
                }
                else if (rawBlock.Metadata.VotingPeriod == "promotion_vote")
                {
                    #region start promotion period
                    Period = new PromotionPeriod
                    {
                        Code = currentPeriod.Code + 1,
                        Epoch = currentEpoch,
                        Kind = VotingPeriods.Promotion,
                        StartLevel = rawBlock.Level,
                        EndLevel = rawBlock.Level + protocol.BlocksPerVoting - 1,
                        Proposal = await Cache.Proposals.GetAsync((currentPeriod as TestingPeriod).ProposalId),
                        ProposalId = (currentPeriod as TestingPeriod).ProposalId
                    };
                    #endregion
                }
                else
                {
                    throw new Exception("invalid voting period");
                }

                if (!(Period is TestingPeriod))
                {
                    var gracePeriod = GracePeriod.Init(block); // TODO: fix crutch
                    var delegates = await Db.Delegates
                        .AsNoTracking()
                        .Where(x => x.Staked && x.DeactivationLevel < gracePeriod && x.StakingBalance >= protocol.TokensPerRoll)
                        .ToListAsync();

                    var lastBlock = await Cache.Blocks.CurrentAsync();
                    lastBlock.Protocol ??= await Cache.Protocols.GetAsync(lastBlock.ProtoCode);

                    Rolls = new List<VotingSnapshot>(delegates.Count);
                    foreach (var delegat in delegates)
                    {
                        Rolls.Add(new VotingSnapshot
                        {
                            Level = lastBlock.Level,
                            Period = Period,
                            DelegateId = delegat.Id,
                            Rolls = (int)(delegat.StakingBalance / lastBlock.Protocol.TokensPerRoll)
                        });
                    }

                    if (Period is ExplorationPeriod exploration)
                        exploration.TotalStake = Rolls.Sum(x => x.Rolls);
                    else if (Period is PromotionPeriod promotion)
                        promotion.TotalStake = Rolls.Sum(x => x.Rolls);
                }
            }
        }

        public async Task Init(Block block)
        {
            if (block.Events.HasFlag(BlockEvents.VotingPeriodEnd))
            {
                Event = BlockEvents.VotingPeriodEnd;
                Period = await Cache.Periods.CurrentAsync();
                Period.Epoch ??= await Db.VotingEpoches.FirstOrDefaultAsync(x => x.Id == Period.EpochId);
            }
            else if (block.Events.HasFlag(BlockEvents.VotingPeriodBegin))
            {
                Event = BlockEvents.VotingPeriodBegin;
                Period = await Cache.Periods.CurrentAsync();
                Period.Epoch ??= await Db.VotingEpoches.FirstOrDefaultAsync(x => x.Id == Period.EpochId);
                if (Period is ExplorationPeriod exploration)
                    exploration.Proposal ??= await Cache.Proposals.GetAsync(exploration.ProposalId);
                if (Period is TestingPeriod testing)
                    testing.Proposal ??= await Cache.Proposals.GetAsync(testing.ProposalId);
                else if (Period is PromotionPeriod promotion)
                    promotion.Proposal ??= await Cache.Proposals.GetAsync(promotion.ProposalId);

                if (!(Period is TestingPeriod))
                    Rolls = await Db.VotingSnapshots.Where(x => x.Level == block.Level - 1).ToListAsync();
            }
        }

        public override Task Apply()
        {
            if (Event == BlockEvents.VotingPeriodEnd)
            {
                #region entities
                var epoch = Period.Epoch;

                Db.TryAttach(epoch);
                #endregion

                epoch.Progress++;
            }
            else if (Event == BlockEvents.VotingPeriodBegin)
            {
                #region entities
                if (Period is ExplorationPeriod exploration)
                    Db.TryAttach(exploration.Proposal);
                else if (Period is TestingPeriod testing)
                    Db.TryAttach(testing.Proposal);
                else if (Period is PromotionPeriod promotion)
                    Db.TryAttach(promotion.Proposal);
                #endregion

                Db.VotingPeriods.Add(Period);
                Cache.Periods.Add(Period);

                if (Rolls != null)
                    Db.VotingSnapshots.AddRange(Rolls);
            }

            return Task.CompletedTask;
        }

        public override Task Revert()
        {
            if (Event == BlockEvents.VotingPeriodEnd)
            {
                #region entities
                var epoch = Period.Epoch;

                Db.TryAttach(epoch);
                #endregion

                epoch.Progress--;
            }
            else if (Event == BlockEvents.VotingPeriodBegin)
            {
                #region entities
                if (Period is ExplorationPeriod exploration)
                    Db.TryAttach(exploration.Proposal);
                else if (Period is TestingPeriod testing)
                    Db.TryAttach(testing.Proposal);
                else if (Period is PromotionPeriod promotion)
                    Db.TryAttach(promotion.Proposal);
                #endregion

                if (Period.Epoch.Progress == 0)
                    Db.VotingEpoches.Remove(Period.Epoch);

                Db.VotingPeriods.Remove(Period);
                Cache.Periods.Remove();

                if (Rolls != null)
                    Db.VotingSnapshots.RemoveRange(Rolls);
            }

            return Task.CompletedTask;
        }

        #region static
        public static async Task<VotingCommit> Apply(ProtocolHandler proto, Block block, RawBlock rawBlock)
        {
            var commit = new VotingCommit(proto);
            await commit.Init(block, rawBlock);
            await commit.Apply();

            return commit;
        }

        public static async Task<VotingCommit> Revert(ProtocolHandler proto, Block block)
        {
            var commit = new VotingCommit(proto);
            await commit.Init(block);
            await commit.Revert();

            return commit;
        }
        #endregion
    }
}
