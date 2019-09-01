using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Tzkt.Data.Models
{
    public class Proposal
    {
        public int Id { get; set; }
        public string Hash { get; set; }
        public ProposalStatus Status { get; set; }

        public int? InitiatorId { get; set; }

        public int? ProposalPeriodId { get; set; }
        public int? ExplorationPeriodId { get; set; }
        public int? TestingPeriodId { get; set; }
        public int? PromotionPeriodId { get; set; }

        #region relations
        [ForeignKey(nameof(InitiatorId))]
        public Contract Initiator { get; set; }

        [ForeignKey(nameof(ProposalPeriodId))]
        public ProposalPeriod ProposalPeriod { get; set; }

        [ForeignKey(nameof(ExplorationPeriodId))]
        public ExplorationPeriod ExplorationPeriod { get; set; }

        [ForeignKey(nameof(TestingPeriodId))]
        public TestingPeriod TestingPeriod { get; set; }

        [ForeignKey(nameof(PromotionPeriodId))]
        public PromotionPeriod PromotionPeriod { get; set; }

        public List<BallotOperation> Ballots { get; set; }
        public List<ProposalOperation> Proposals { get; set; }
        #endregion
    }

    public enum ProposalStatus
    {
        Active,
        Applied,
        Declined
    }
}
