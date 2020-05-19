using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto3
{
    class ProposalsCommit : ProtocolCommit
    {
        public List<ProposalOperation> ProposalOperations { get; private set; }

        ProposalsCommit(ProtocolHandler protocol) : base(protocol) { }

        public async Task Init(Block block, RawOperation op, RawProposalContent content)
        {
            var period = (ProposalPeriod)await Cache.Periods.CurrentAsync();
            var sender = Cache.Accounts.GetDelegate(content.Source);
            var rolls = (await Db.VotingSnapshots.FirstAsync(x => x.PeriodId == period.Id && x.DelegateId == sender.Id)).Rolls;

            ProposalOperations = new List<ProposalOperation>(4);
            foreach (var proposalHash in content.Proposals)
            {
                var proposal = await Cache.Proposals.GetOrCreateAsync(proposalHash, () => new Proposal
                {
                    Hash = proposalHash,
                    Initiator = sender,
                    ProposalPeriod = period,
                    Status = ProposalStatus.Active
                });

                var duplicated = ProposalOperations.Any(x => x.Period.Id == period.Id && x.Sender.Id == sender.Id && x.Proposal.Hash == proposal.Hash);
                if (!duplicated) duplicated = block.Proposals?.Any(x => x.Period.Id == period.Id && x.Sender.Id == sender.Id && x.Proposal.Hash == proposal.Hash) ?? false;
                if (!duplicated) duplicated = await Db.ProposalOps.AnyAsync(x => x.PeriodId == period.Id && x.SenderId == sender.Id && x.ProposalId == proposal.Id);

                ProposalOperations.Add(new ProposalOperation
                {
                    Id = Cache.AppState.NextOperationId(),
                    Block = block,
                    Level = block.Level,
                    Timestamp = block.Timestamp,
                    OpHash = op.Hash,
                    Sender = sender,
                    Rolls = rolls,
                    Duplicated = duplicated,
                    Period = period,
                    Proposal = proposal
                });
            }
        }

        public async Task Init(Block block, ProposalOperation proposal)
        {
            proposal.Block ??= block;
            proposal.Sender ??= Cache.Accounts.GetDelegate(proposal.SenderId);
            proposal.Period ??= await Cache.Periods.CurrentAsync();
            proposal.Proposal ??= await Cache.Proposals.GetAsync(proposal.ProposalId);

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
                if (!proposalOp.Duplicated)
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
                if (!proposalOp.Duplicated)
                    proposal.Upvotes -= proposalOp.Rolls;

                sender.ProposalsCount--;
                #endregion

                if (proposal.Upvotes == 0)
                {
                    Db.Proposals.Remove(proposal);
                    Cache.Proposals.Remove(proposal);
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
