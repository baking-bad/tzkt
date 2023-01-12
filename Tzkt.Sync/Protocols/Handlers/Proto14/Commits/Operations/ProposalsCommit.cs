using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto14
{
    class ProposalsCommit : Proto3.ProposalsCommit
    {
        public bool DictatorSeen = false;

        public ProposalsCommit(ProtocolHandler protocol) : base(protocol) { }

        public override async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            if (content.RequiredString("source") != block.Protocol.Dictator)
            {
                await base.Apply(block, op, content);
                return;
            }

            Logger.LogWarning("Governance dictator is resetting the current voting epoch. All the voting history will be irrevocably removed from the database.");

            // Dictator's actions cause one-way changes, so in case of reorg the indexer won't be able to revert them
            DictatorSeen = true;

            var sender = await Cache.Accounts.GetAsync(content.RequiredString("source"));
            var period = await Cache.Periods.GetAsync(content.RequiredInt32("period"));
            var proposalHashes = content.RequiredArray("proposals").EnumerateArray().Select(x => x.RequiredString()).ToList();

            #region remove voting operations
            if (period.Kind == PeriodKind.Proposal)
            {
                var proposalOps = (await Db.ProposalOps.Where(x => x.Period == period.Index).ToListAsync())
                    .Concat(Db.ChangeTracker.Entries().Where(x => x.Entity is ProposalOperation && x.State == EntityState.Added).Select(x => x.Entity as ProposalOperation));

                foreach (var proposalOp in proposalOps)
                {
                    var proposalSender = Cache.Accounts.GetDelegate(proposalOp.SenderId);
                    Db.TryAttach(proposalSender);
                    proposalSender.ProposalsCount--;

                    var proposalBlock = await Cache.Blocks.GetAsync(proposalOp.Level);
                    Db.TryAttach(proposalBlock);
                    proposalBlock.Operations &= ~Operations.Proposals;

                    Cache.AppState.Get().ProposalOpsCount--;

                    Db.ProposalOps.Remove(proposalOp);
                }
            }
            else if (period.Kind == PeriodKind.Exploration || period.Kind == PeriodKind.Promotion)
            {
                var ballotOps = (await Db.BallotOps.Where(x => x.Period == period.Index).ToListAsync())
                    .Concat(Db.ChangeTracker.Entries().Where(x => x.Entity is BallotOperation && x.State == EntityState.Added).Select(x => x.Entity as BallotOperation));
                
                foreach (var ballotOp in ballotOps)
                {
                    var ballotSender = Cache.Accounts.GetDelegate(ballotOp.SenderId);
                    Db.TryAttach(ballotSender);
                    ballotSender.BallotsCount--;

                    var ballotBlock = await Cache.Blocks.GetAsync(ballotOp.Level);
                    Db.TryAttach(ballotBlock);
                    ballotBlock.Operations &= ~Operations.Ballots;

                    Cache.AppState.Get().BallotOpsCount--;

                    Db.BallotOps.Remove(ballotOp);
                }
            }
            #endregion

            #region remove proposals
            var proposals = (await Db.Proposals.Where(x => x.FirstPeriod == period.Index).ToListAsync())
                .Concat(Db.ChangeTracker.Entries().Where(x => x.Entity is Proposal && x.State == EntityState.Added).Select(x => x.Entity as Proposal));

            foreach (var proposal in proposals)
            {
                Cache.AppState.Get().ProposalsCount--;
                Db.Proposals.Remove(proposal);
            }

            if (period.Kind != PeriodKind.Proposal && period.Dictator < DictatorStatus.Reset)
            {
                var activeProposal = await Db.Proposals.FirstOrDefaultAsync(x => x.FirstPeriod != period.Index && x.LastPeriod == period.Index);
                if (activeProposal != null)
                {
                    activeProposal.LastPeriod--;
                    activeProposal.Status = ProposalStatus.Rejected;
                }
            }

            Cache.Proposals.Reset();
            #endregion

            #region reset snapshots
            var snapshots = await Db.VotingSnapshots.Where(x => x.Period == period.Index).ToListAsync();
            foreach (var snapshot in snapshots)
                snapshot.Status = VoterStatus.None;
            #endregion

            #region reset periods
            Db.TryAttach(period);

            if (period.Kind != PeriodKind.Proposal && period.Dictator < DictatorStatus.Reset)
            {
                period.Epoch++;

                var prevPeriod = await Cache.Periods.GetAsync(period.Index - 1);
                Db.TryAttach(prevPeriod);
                prevPeriod.Dictator = DictatorStatus.Abort;
            }

            if (proposalHashes.Count == 0)
            {
                period.Kind = PeriodKind.Proposal;
                period.Dictator = DictatorStatus.Reset;

                period.TotalBakers = snapshots.Count;
                period.TotalVotingPower = snapshots.Sum(x => x.VotingPower);

                period.UpvotesQuorum = block.Protocol.ProposalQuorum;
                period.ProposalsCount = 0;
                period.TopUpvotes = 0;
                period.TopVotingPower = 0;
                period.SingleWinner = false;
            }
            else
            {
                period.Kind = PeriodKind.Adoption;
                period.Dictator = DictatorStatus.Submit;

                period.TotalBakers = null;
                period.TotalVotingPower = null;

                period.UpvotesQuorum = null;
                period.ProposalsCount = null;
                period.TopUpvotes = null;
                period.TopVotingPower = null;
                period.SingleWinner = null;
            }

            period.ParticipationEma = null;
            period.BallotsQuorum = null;
            period.Supermajority = null;

            period.YayBallots = null;
            period.NayBallots = null;
            period.PassBallots = null;
            period.YayVotingPower = null;
            period.NayVotingPower = null;
            period.PassVotingPower = null;

            Cache.Periods.Reset();
            #endregion

            #region push proposal
            if (proposalHashes.Count > 0)
            {
                var proposal = new Proposal
                {
                    Epoch = period.Epoch,
                    FirstPeriod = period.Index,
                    LastPeriod = period.Index,
                    Hash = proposalHashes[0],
                    InitiatorId = sender.Id,
                    Status = ProposalStatus.Active,
                    Upvotes = 0,
                    VotingPower = 0
                };
                Cache.AppState.Get().ProposalsCount++;
                Db.Proposals.Add(proposal);
            }
            #endregion
        }
    }
}
