using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto3
{
    class ProposalsCommit : ProtocolCommit
    {
        public ProposalsCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var period = (ProposalPeriod)await Cache.Periods.CurrentAsync();
            var sender = Cache.Accounts.GetDelegate(content.RequiredString("source"));
            
            var rolls = 0;
            if (block.Events.HasFlag(BlockEvents.VotingPeriodBegin))
                rolls = (int)(sender.StakingBalance / block.Protocol.TokensPerRoll);
            else
                rolls = (await Db.VotingSnapshots.FirstAsync(x => x.PeriodId == period.Id && x.DelegateId == sender.Id)).Rolls;

            var proposalOperations = new List<ProposalOperation>(4);
            foreach (var proposalHash in content.RequiredArray("proposals").EnumerateArray())
            {
                var proposal = await Cache.Proposals.GetOrCreateAsync(proposalHash.RequiredString(), () => new Proposal
                {
                    Hash = proposalHash.RequiredString(),
                    Initiator = sender,
                    ProposalPeriod = period,
                    Status = ProposalStatus.Active
                });

                var duplicated = proposalOperations.Any(x => x.Period.Id == period.Id && x.Sender.Id == sender.Id && x.Proposal.Hash == proposal.Hash);
                if (!duplicated) duplicated = block.Proposals?.Any(x => x.Period.Id == period.Id && x.Sender.Id == sender.Id && x.Proposal.Hash == proposal.Hash) ?? false;
                if (!duplicated) duplicated = await Db.ProposalOps.AnyAsync(x => x.PeriodId == period.Id && x.SenderId == sender.Id && x.ProposalId == proposal.Id);

                proposalOperations.Add(new ProposalOperation
                {
                    Id = Cache.AppState.NextOperationId(),
                    Block = block,
                    Level = block.Level,
                    Timestamp = block.Timestamp,
                    OpHash = op.RequiredString("hash"),
                    Sender = sender,
                    Rolls = rolls,
                    Duplicated = duplicated,
                    Period = period,
                    Proposal = proposal
                });
            }
            #endregion

            foreach (var proposalOp in proposalOperations)
            {
                #region entities
                //var block = proposalOp.Block;
                //var period = proposalOp.Period;
                //var sender = proposalOp.Sender;
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
        }

        public virtual async Task Revert(Block block, ProposalOperation proposalOp)
        {
            #region init
            proposalOp.Block ??= block;
            proposalOp.Sender ??= Cache.Accounts.GetDelegate(proposalOp.SenderId);
            proposalOp.Period ??= await Cache.Periods.CurrentAsync();
            proposalOp.Proposal ??= await Cache.Proposals.GetAsync(proposalOp.ProposalId);
            #endregion

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
    }
}
