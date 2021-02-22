using System;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto8
{
    class VotingCommit : Proto5.VotingCommit
    {
        public VotingCommit(ProtocolHandler protocol) : base(protocol) { }

        // new voting period
        protected override ProposalStatus GetProposalStatus(Proposal proposal, VotingPeriod period)
        {
            if (period.Status == PeriodStatus.Success)
                return period.Kind == PeriodKind.Adoption
                    ? ProposalStatus.Accepted
                    : ProposalStatus.Active;

            if (period.Status == PeriodStatus.NoSupermajority)
                return ProposalStatus.Rejected;

            return ProposalStatus.Skipped;
        }

        // new voting period
        protected override VotingPeriod StartNextPeriod(Block block, VotingPeriod current)
        {
            return current.Kind switch
            {
                PeriodKind.Proposal => StartBallotPeriod(block, current, PeriodKind.Exploration),
                PeriodKind.Exploration => StartWaitingPeriod(block, current, PeriodKind.Testing),
                PeriodKind.Testing => StartBallotPeriod(block, current, PeriodKind.Promotion),
                PeriodKind.Promotion => StartWaitingPeriod(block, current, PeriodKind.Adoption),
                PeriodKind.Adoption => StartProposalPeriod(block, current),
                _ => throw new Exception("Invalid voting period kind")
            };
        }
    }
}
