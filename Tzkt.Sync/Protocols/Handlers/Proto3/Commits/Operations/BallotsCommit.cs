using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto3
{
    class BallotsCommit : ProtocolCommit
    {
        public BallotsCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var period = await Cache.Periods.CurrentAsync();
            var proposal = await Cache.Proposals.GetAsync((period as ExplorationPeriod)?.ProposalId ?? (period as PromotionPeriod).ProposalId);
            var sender = Cache.Accounts.GetDelegate(content.RequiredString("source"));

            var rolls = 0;
            if (block.Events.HasFlag(BlockEvents.VotingPeriodBegin))
                rolls = (int)(sender.StakingBalance / block.Protocol.TokensPerRoll);
            else
                rolls = (await Db.VotingSnapshots.FirstAsync(x => x.PeriodId == period.Id && x.DelegateId == sender.Id)).Rolls;

            var ballot = new BallotOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Block = block,
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = op.RequiredString("hash"),
                Sender = sender,
                Rolls = rolls,
                Period = period,
                Proposal = proposal,
                Vote = content.RequiredString("ballot") switch
                {
                    "yay" => Vote.Yay,
                    "nay" => Vote.Nay,
                    "pass" => Vote.Pass,
                    _ => throw new Exception("invalid ballot value")
                }
            };
            #endregion

            #region entities
            //var block = ballot.Block;
            //var period = ballot.Period;
            //var sender = ballot.Sender;
            //var proposal = ballot.Proposal;

            //Db.TryAttach(block);
            Db.TryAttach(period);
            Db.TryAttach(sender);
            Db.TryAttach(proposal);
            #endregion

            #region apply operation
            if (period is ExplorationPeriod exploration)
            {
                exploration.Abstainings += ballot.Vote == Vote.Pass ? ballot.Rolls : 0;
                exploration.Approvals += ballot.Vote == Vote.Yay ? ballot.Rolls : 0;
                exploration.Refusals += ballot.Vote == Vote.Nay ? ballot.Rolls : 0;
                exploration.Participation += ballot.Rolls;
            }
            else if (period is PromotionPeriod promotion)
            {
                promotion.Abstainings += ballot.Vote == Vote.Pass ? ballot.Rolls : 0;
                promotion.Approvals += ballot.Vote == Vote.Yay ? ballot.Rolls : 0;
                promotion.Refusals += ballot.Vote == Vote.Nay ? ballot.Rolls : 0;
                promotion.Participation += ballot.Rolls;
            }

            sender.BallotsCount++;

            block.Operations |= Operations.Ballots;
            #endregion

            Db.BallotOps.Add(ballot);
        }

        public virtual async Task Revert(Block block, BallotOperation ballot)
        {
            #region init
            ballot.Block ??= block;
            ballot.Sender ??= Cache.Accounts.GetDelegate(ballot.SenderId);
            ballot.Period ??= await Cache.Periods.CurrentAsync();
            ballot.Proposal ??= await Cache.Proposals.GetAsync(ballot.ProposalId);
            #endregion

            #region entities
            //var block = proposal.Block;
            var sender = ballot.Sender;
            var period = ballot.Period;
            var proposal = ballot.Proposal;

            //Db.TryAttach(block);
            Db.TryAttach(sender);
            Db.TryAttach(period);
            Db.TryAttach(proposal);
            #endregion

            #region revert operation
            if (period is ExplorationPeriod exploration)
            {
                exploration.Abstainings -= ballot.Vote == Vote.Pass ? ballot.Rolls : 0;
                exploration.Approvals -= ballot.Vote == Vote.Yay ? ballot.Rolls : 0;
                exploration.Refusals -= ballot.Vote == Vote.Nay ? ballot.Rolls : 0;
                exploration.Participation -= ballot.Rolls;
            }
            else if (period is PromotionPeriod promotion)
            {
                promotion.Abstainings -= ballot.Vote == Vote.Pass ? ballot.Rolls : 0;
                promotion.Approvals -= ballot.Vote == Vote.Yay ? ballot.Rolls : 0;
                promotion.Refusals -= ballot.Vote == Vote.Nay ? ballot.Rolls : 0;
                promotion.Participation -= ballot.Rolls;
            }

            sender.BallotsCount--;
            #endregion

            Db.BallotOps.Remove(ballot);
        }

        public override Task Apply() => Task.CompletedTask;
        public override Task Revert() => Task.CompletedTask;
    }
}
