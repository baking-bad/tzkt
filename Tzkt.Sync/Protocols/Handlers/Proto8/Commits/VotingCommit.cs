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
        protected override VotingPeriod StartNextPeriod(Block block, Protocol protocol, VotingPeriod current)
        {
            return current.Kind switch
            {
                PeriodKind.Proposal => StartBallotPeriod(block, protocol, current, PeriodKind.Exploration),
                PeriodKind.Exploration => StartWaitingPeriod(block, protocol, current, PeriodKind.Testing),
                PeriodKind.Testing => StartBallotPeriod(block, protocol, current, PeriodKind.Promotion),
                PeriodKind.Promotion => StartWaitingPeriod(block, protocol, current, PeriodKind.Adoption),
                PeriodKind.Adoption => StartProposalPeriod(block, protocol, current),
                _ => throw new Exception("Invalid voting period kind")
            };
        }
    }
}
