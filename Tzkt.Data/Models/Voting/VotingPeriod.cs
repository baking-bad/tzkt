using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tzkt.Data.Models
{
    public abstract class VotingPeriod
    {
        public int Id { get; set; }
        public int EpochId { get; set; }
        public VotingPeriods Kind { get; set; }
        public int StartLevel { get; set; }
        public int EndLevel { get; set; }

        #region relations
        [ForeignKey(nameof(EpochId))]
        public VotingEpoch Epoch { get; set; }

        public List<BallotOperation> Ballots { get; set; }
        public List<ProposalOperation> Proposals { get; set; }
        #endregion
    }

    public enum VotingPeriods
    {
        Proposal,
        Exploration,
        Testing,
        Promotion
    }
}
