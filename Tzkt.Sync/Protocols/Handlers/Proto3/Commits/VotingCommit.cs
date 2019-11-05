using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto3
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
                Period = await Cache.GetCurrentVotingPeriodAsync();
                Period.Epoch ??= await Db.VotingEpoches.FirstOrDefaultAsync(x => x.Id == Period.EpochId);
            }
            else if (block.Events.HasFlag(BlockEvents.VotingPeriodBegin))
            {
                Event = BlockEvents.VotingPeriodBegin;
                var protocol = await Cache.GetProtocolAsync(rawBlock.Protocol);

                var currentPeriod = await Cache.GetCurrentVotingPeriodAsync();
                var currentEpoch = await Db.VotingEpoches.FirstOrDefaultAsync(x => x.Id == currentPeriod.EpochId);

                if (rawBlock.Metadata.VotingPeriod == "proposal")
                {
                    #region start proposal period
                    Period = new ProposalPeriod
                    {
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
                        .OrderByDescending(x => x.Likes)
                        .FirstAsync();

                    Cache.AddProposal(proposal);

                    Period = new ExplorationPeriod
                    {
                        Epoch = currentEpoch,
                        Kind = VotingPeriods.Exploration,
                        StartLevel = rawBlock.Level,
                        EndLevel = rawBlock.Level + protocol.BlocksPerVoting - 1,
                        Proposal = proposal,
                    };
                    #endregion
                }
                else if (rawBlock.Metadata.VotingPeriod == "testing")
                {
                    #region start testing period
                    Period = new TestingPeriod
                    {
                        Epoch = currentEpoch,
                        Kind = VotingPeriods.Testing,
                        StartLevel = rawBlock.Level,
                        EndLevel = rawBlock.Level + protocol.BlocksPerVoting - 1,
                        Proposal = (currentPeriod as ExplorationPeriod).Proposal
                    };
                    #endregion
                }
                else if (rawBlock.Metadata.VotingPeriod == "promotion")
                {
                    #region start promotion period
                    Period = new PromotionPeriod
                    {
                        Epoch = currentEpoch,
                        Kind = VotingPeriods.Promotion,
                        StartLevel = rawBlock.Level,
                        EndLevel = rawBlock.Level + protocol.BlocksPerVoting - 1,
                        Proposal = (currentPeriod as TestingPeriod).Proposal
                    };
                    #endregion
                }
                else
                {
                    throw new Exception("invalid voting period");
                }

                var gracePeriod = GracePeriod.Init(block); // TODO: fix crutch
                var delegates = await Db.Delegates
                    .AsNoTracking()
                    .Where(x => x.Staked && x.DeactivationLevel < gracePeriod && x.StakingBalance >= protocol.TokensPerRoll)
                    .ToListAsync();

                Rolls = new List<VotingSnapshot>(delegates.Count);
                foreach (var delegat in delegates)
                {
                    Rolls.Add(new VotingSnapshot
                    {
                        Level = block.Level - 1,
                        Period = Period,
                        DelegateId = delegat.Id,
                        Rolls = (int)(delegat.StakingBalance / block.Protocol.TokensPerRoll)
                    });
                }

                if (Period is ExplorationPeriod exploration)
                    exploration.TotalStake = Rolls.Sum(x => x.Rolls);
                else if (Period is PromotionPeriod promotion)
                    promotion.TotalStake = Rolls.Sum(x => x.Rolls);
            }
        }

        public async Task Init(Block block)
        {
            if (block.Events.HasFlag(BlockEvents.VotingPeriodEnd))
            {
                Event = BlockEvents.VotingPeriodEnd;
                Period = await Cache.GetCurrentVotingPeriodAsync();
                Period.Epoch ??= await Db.VotingEpoches.FirstOrDefaultAsync(x => x.Id == Period.EpochId);
            }
            else if (block.Events.HasFlag(BlockEvents.VotingPeriodBegin))
            {
                Event = BlockEvents.VotingPeriodBegin;
                Period = await Cache.GetCurrentVotingPeriodAsync();
                Period.Epoch ??= await Db.VotingEpoches.FirstOrDefaultAsync(x => x.Id == Period.EpochId);
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
                if (Period is ExplorationPeriod exploration)
                    Db.TryAttach(exploration.Proposal);

                Db.VotingPeriods.Add(Period);
                Cache.AddVotingPeriod(Period);

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
                if (Period.Epoch.Progress == 0)
                    Db.VotingEpoches.Remove(Period.Epoch);

                Db.VotingPeriods.Remove(Period);
                Cache.RemoveVotingPeriod();

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
