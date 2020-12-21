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
            switch (current.Kind)
            {
                case PeriodKind.Proposal:
                    return StartBallotPeriod(block, current, PeriodKind.Exploration);
                case PeriodKind.Exploration:
                    return StartWaitingPeriod(block, current, PeriodKind.Testing);
                case PeriodKind.Testing:
                    return StartBallotPeriod(block, current, PeriodKind.Promotion);
                case PeriodKind.Promotion:
                    return StartWaitingPeriod(block, current, PeriodKind.Adoption);
                case PeriodKind.Adoption:
                    return StartProposalPeriod(block, current);
                default:
                    throw new Exception("Invalid voting period kind");
            }
        }
    }
}
