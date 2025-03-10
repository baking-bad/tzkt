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
            var period = await Cache.Periods.GetAsync(content.RequiredInt32("period"));
            Db.TryAttach(period);
            var sender = Cache.Accounts.GetDelegate(content.RequiredString("source"));
            Db.TryAttach(sender);
            
            var snapshot = await Db.VotingSnapshots
                .FirstOrDefaultAsync(x => x.Period == period.Index && x.BakerId == sender.Id)
                    ?? throw new ValidationException("Proposal sender is not on the voters list");

            foreach (var proposalHash in content.RequiredArray("proposals").EnumerateArray())
            {
                var proposal = await Cache.Proposals.GetOrCreateAsync(period.Epoch, proposalHash.RequiredString(), () => new Proposal
                {
                    Id = Cache.AppState.NextProposalId(),
                    Hash = proposalHash.RequiredString(),
                    Epoch = period.Epoch,
                    FirstPeriod = period.Index,
                    LastPeriod = period.Index,
                    InitiatorId = sender.Id,
                    Status = ProposalStatus.Active
                });

                if (proposal.Upvotes == 0)
                    Db.Proposals.Add(proposal);
                else
                    Db.TryAttach(proposal);

                var duplicated = Context.ProposalOps.Any(x => x.Period == period.Index && x.SenderId == sender.Id && x.ProposalId == proposal.Id);
                if (!duplicated) duplicated = await Db.ProposalOps.AnyAsync(x => x.Period == period.Index && x.SenderId == sender.Id && x.ProposalId == proposal.Id);

                var proposalOp = new ProposalOperation
                {
                    Id = Cache.AppState.NextOperationId(),
                    Level = block.Level,
                    Timestamp = block.Timestamp,
                    OpHash = op.RequiredString("hash"),
                    SenderId = sender.Id,
                    VotingPower = snapshot.VotingPower,
                    Duplicated = duplicated,
                    Epoch = period.Epoch,
                    Period = period.Index,
                    ProposalId = proposal.Id,
                };

                #region apply operation
                if (!proposalOp.Duplicated)
                {
                    if (proposal.Upvotes == 0)
                    {
                        period.ProposalsCount++;
                    }

                    proposal.Upvotes++;
                    proposal.VotingPower += proposalOp.VotingPower;

                    if (proposal.VotingPower > period.TopVotingPower)
                    {
                        period.TopUpvotes = proposal.Upvotes;
                        period.TopVotingPower = proposal.VotingPower;
                        period.SingleWinner = true;
                    }
                    else if (proposal.VotingPower == period.TopVotingPower)
                    {
                        period.SingleWinner = false;
                    }

                    snapshot.Status = VoterStatus.Upvoted;
                }

                sender.ProposalsCount++;

                block.Operations |= Operations.Proposals;

                Cache.AppState.Get().ProposalOpsCount++;
                #endregion

                Db.ProposalOps.Add(proposalOp);
                Context.ProposalOps.Add(proposalOp);
            }
        }

        public virtual async Task Revert(Block block, ProposalOperation proposalOp)
        {
            #region entities
            var sender = Cache.Accounts.GetDelegate(proposalOp.SenderId);
            var proposal = await Cache.Proposals.GetAsync(proposalOp.ProposalId);

            var snapshot = await Db.VotingSnapshots
                .FirstAsync(x => x.Period == proposalOp.Period && x.BakerId == proposalOp.SenderId);

            var period = await Cache.Periods.GetAsync(proposalOp.Period);

            Db.TryAttach(period);
            Db.TryAttach(sender);
            Db.TryAttach(proposal);
            #endregion

            #region revert operation
            if (!proposalOp.Duplicated)
            {
                proposal.Upvotes--;
                proposal.VotingPower -= proposalOp.VotingPower;

                if (period.ProposalsCount > 1)
                {
                    var proposals = await Db.Proposals
                        .Where(x => x.Epoch == period.Epoch)
                        .ToListAsync();

                    var prevMax = proposals
                        .OrderByDescending(x => x.VotingPower)
                        .First();

                    period.TopUpvotes = prevMax.Upvotes;
                    period.TopVotingPower = prevMax.VotingPower;
                    period.SingleWinner = proposals.Count(x => x.VotingPower == period.TopVotingPower) == 1;
                }
                else
                {
                    period.TopUpvotes = proposal.Upvotes;
                    period.TopVotingPower = proposal.VotingPower;
                    period.SingleWinner = proposal.VotingPower > 0;
                }

                if (proposal.Upvotes == 0)
                {
                    period.ProposalsCount--;
                }

                if (!await Db.ProposalOps.AnyAsync(x => x.Period == period.Index && x.SenderId == sender.Id && x.Id < proposalOp.Id))
                    snapshot.Status = VoterStatus.None;
            }

            sender.ProposalsCount--;

            Cache.AppState.Get().ProposalOpsCount--;
            #endregion

            if (proposal.Upvotes == 0)
            {
                Db.Proposals.Remove(proposal);
                Cache.Proposals.Remove(proposal);
                Cache.AppState.ReleaseProposalId();
            }

            Db.ProposalOps.Remove(proposalOp);
            Cache.AppState.ReleaseOperationId();
        }
    }
}
