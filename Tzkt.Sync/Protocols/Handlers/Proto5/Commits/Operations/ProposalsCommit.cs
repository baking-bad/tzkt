using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto5
{
    class ProposalsCommit : ProtocolCommit
    {
        public List<ProposalOperation> ProposalOperations { get; private set; }

        ProposalsCommit(ProtocolHandler protocol) : base(protocol) { }

        public async Task Init(Block block, RawOperation op, RawProposalContent content)
        {
            var period = (ProposalPeriod)await Cache.GetCurrentVotingPeriodAsync();
            var sender = await Cache.GetDelegateAsync(content.Source);
            var rolls = (await Db.VotingSnapshots.FirstAsync(x => x.PeriodId == period.Id && x.DelegateId == sender.Id)).Rolls;

            ProposalOperations = new List<ProposalOperation>(4);
            foreach (var proposalHash in content.Proposals)
            {
                var proposal = await Cache.GetOrSetProposalAsync(proposalHash, () => Task.FromResult(new Proposal
                {
                    Hash = proposalHash,
                    Initiator = sender,
                    ProposalPeriod = period,
                    Status = ProposalStatus.Active
                }));

                var redundant = ProposalOperations.Any(x => x.Period.Id == period.Id && x.Sender.Id == sender.Id && x.Proposal.Hash == proposal.Hash);
                if (!redundant) redundant = block.Proposals?.Any(x => x.Period.Id == period.Id && x.Sender.Id == sender.Id && x.Proposal.Hash == proposal.Hash) ?? false;
                if (!redundant) redundant = await Db.ProposalOps.AnyAsync(x => x.PeriodId == period.Id && x.SenderId == sender.Id && x.ProposalId == proposal.Id);

                ProposalOperations.Add(new ProposalOperation
                {
                    Id = await Cache.NextCounterAsync(),
                    Block = block,
                    Level = block.Level,
                    Timestamp = block.Timestamp,
                    OpHash = op.Hash,
                    Sender = sender,
                    Rolls = rolls,
                    Redundant = redundant,
                    Period = period,
                    Proposal = proposal
                });
            }
        }

        public async Task Init(Block block, ProposalOperation proposal)
        {
            proposal.Block ??= block;
            proposal.Sender ??= (Data.Models.Delegate)await Cache.GetAccountAsync(proposal.SenderId);
            proposal.Period ??= await Cache.GetCurrentVotingPeriodAsync();
            proposal.Proposal ??= await Cache.GetProposalAsync(proposal.ProposalId);

            ProposalOperations = new List<ProposalOperation> { proposal };
        }

        public override Task Apply()
        {
            foreach (var proposalOp in ProposalOperations)
            {
                #region entities
                var block = proposalOp.Block;
                var period = proposalOp.Period;
                var sender = proposalOp.Sender;
                var proposal = proposalOp.Proposal;

                //Db.TryAttach(block);
                Db.TryAttach(period);
                Db.TryAttach(sender);
                Db.TryAttach(proposal);
                #endregion

                #region apply operation
                if (!proposalOp.Redundant)
                    proposal.Upvotes += proposalOp.Rolls;

                sender.ProposalsCount++;

                block.Operations |= Operations.Proposals;
                #endregion

                Db.ProposalOps.Add(proposalOp);
            }

            return Task.CompletedTask;
        }

        public override Task Revert()
        {
            foreach (var proposalOp in ProposalOperations)
            {
                #region entities
                //var block = proposal.Block;
                var sender = proposalOp.Sender;
                var proposal = proposalOp.Proposal;

                //Db.TryAttach(block);
                Db.TryAttach(sender);
                Db.TryAttach(proposal);
                #endregion

                #region revert operation
                if (!proposalOp.Redundant)
                    proposal.Upvotes -= proposalOp.Rolls;

                sender.ProposalsCount--;
                #endregion

                if (proposal.Upvotes == 0)
                {
                    Db.Proposals.Remove(proposal);
                    Cache.RemoveProposal(proposal);
                }

                Db.ProposalOps.Remove(proposalOp);
            }

            return Task.CompletedTask;
        }

        #region static
        public static async Task<ProposalsCommit> Apply(ProtocolHandler proto, Block block, RawOperation op, RawProposalContent content)
        {
            var commit = new ProposalsCommit(proto);
            await commit.Init(block, op, content);
            await commit.Apply();

            return commit;
        }

        public static async Task<ProposalsCommit> Revert(ProtocolHandler proto, Block block, ProposalOperation op)
        {
            var commit = new ProposalsCommit(proto);
            await commit.Init(block, op);
            await commit.Revert();

            return commit;
        }
        #endregion
    }
}
