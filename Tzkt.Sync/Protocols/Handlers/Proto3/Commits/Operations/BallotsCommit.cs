using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto3
{
    class BallotsCommit : ProtocolCommit
    {
        public BallotOperation Ballot  { get; private set; }

        BallotsCommit(ProtocolHandler protocol) : base(protocol) { }

        public async Task Init(Block block, RawOperation op, RawBallotContent content)
        {
            var period = await Cache.Periods.CurrentAsync();
            var proposal = await Cache.Proposals.GetAsync((period as ExplorationPeriod)?.ProposalId ?? (period as PromotionPeriod).ProposalId);
            var sender = Cache.Accounts.GetDelegate(content.Source);
            var rolls = (await Db.VotingSnapshots.FirstAsync(x => x.PeriodId == period.Id && x.DelegateId == sender.Id)).Rolls;

            Ballot = new BallotOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Block = block,
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = op.Hash,
                Sender = sender,
                Rolls = rolls,
                Period = period,
                Proposal = proposal,
                Vote = content.Ballot switch
                {
                    "yay" => Vote.Yay,
                    "nay" => Vote.Nay,
                    "pass" => Vote.Pass,
                    _ => throw new Exception("invalid ballot value")
                }
            };
        }

        public async Task Init(Block block, BallotOperation ballot)
        {
            Ballot = ballot;
            Ballot.Block ??= block;
            Ballot.Sender ??= Cache.Accounts.GetDelegate(ballot.SenderId);
            Ballot.Period ??= await Cache.Periods.CurrentAsync();
            Ballot.Proposal ??= await Cache.Proposals.GetAsync(ballot.ProposalId);
        }

        public override Task Apply()
        {
            #region entities
            var block = Ballot.Block;
            var period = Ballot.Period;
            var sender = Ballot.Sender;
            var proposal = Ballot.Proposal;

            //Db.TryAttach(block);
            Db.TryAttach(period);
            Db.TryAttach(sender);
            Db.TryAttach(proposal);
            #endregion

            #region apply operation
            if (period is ExplorationPeriod exploration)
            {
                exploration.Abstainings += Ballot.Vote == Vote.Pass ? Ballot.Rolls : 0;
                exploration.Approvals += Ballot.Vote == Vote.Yay ? Ballot.Rolls : 0;
                exploration.Refusals += Ballot.Vote == Vote.Nay ? Ballot.Rolls : 0;
                exploration.Participation += Ballot.Rolls;
            }
            else if (period is PromotionPeriod promotion)
            {
                promotion.Abstainings += Ballot.Vote == Vote.Pass ? Ballot.Rolls : 0;
                promotion.Approvals += Ballot.Vote == Vote.Yay ? Ballot.Rolls : 0;
                promotion.Refusals += Ballot.Vote == Vote.Nay ? Ballot.Rolls : 0;
                promotion.Participation += Ballot.Rolls;
            }

            sender.BallotsCount++;

            block.Operations |= Operations.Ballots;
            #endregion

            Db.BallotOps.Add(Ballot);

            return Task.CompletedTask;
        }

        public override Task Revert()
        {
            #region entities
            //var block = proposal.Block;
            var sender = Ballot.Sender;
            var period = Ballot.Period;
            var proposal = Ballot.Proposal;

            //Db.TryAttach(block);
            Db.TryAttach(sender);
            Db.TryAttach(period);
            Db.TryAttach(proposal);
            #endregion

            #region revert operation
            if (period is ExplorationPeriod exploration)
            {
                exploration.Abstainings -= Ballot.Vote == Vote.Pass ? Ballot.Rolls : 0;
                exploration.Approvals -= Ballot.Vote == Vote.Yay ? Ballot.Rolls : 0;
                exploration.Refusals -= Ballot.Vote == Vote.Nay ? Ballot.Rolls : 0;
                exploration.Participation -= Ballot.Rolls;
            }
            else if (period is PromotionPeriod promotion)
            {
                promotion.Abstainings -= Ballot.Vote == Vote.Pass ? Ballot.Rolls : 0;
                promotion.Approvals -= Ballot.Vote == Vote.Yay ? Ballot.Rolls : 0;
                promotion.Refusals -= Ballot.Vote == Vote.Nay ? Ballot.Rolls : 0;
                promotion.Participation -= Ballot.Rolls;
            }

            sender.BallotsCount--;
            #endregion

            Db.BallotOps.Remove(Ballot);

            return Task.CompletedTask;
        }

        #region static
        public static async Task<BallotsCommit> Apply(ProtocolHandler proto, Block block, RawOperation op, RawBallotContent content)
        {
            var commit = new BallotsCommit(proto);
            await commit.Init(block, op, content);
            await commit.Apply();

            return commit;
        }

        public static async Task<BallotsCommit> Revert(ProtocolHandler proto, Block block, BallotOperation op)
        {
            var commit = new BallotsCommit(proto);
            await commit.Init(block, op);
            await commit.Revert();

            return commit;
        }
        #endregion
    }
}
