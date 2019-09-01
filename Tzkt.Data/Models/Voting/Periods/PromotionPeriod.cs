using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tzkt.Data.Models
{
    public class PromotionPeriod : VotingPeriod
    {
        public int ProposalId { get; set; }

        public int TotalStake { get; set; }
        public int Participation { get; set; }
        public int Quorum { get; set; }

        public int Abstainings { get; set; }
        public int Approvals { get; set; }
        public int Refusals { get; set; }

        #region relations
        [ForeignKey(nameof(ProposalId))]
        public Proposal Proposal { get; set; }
        #endregion
    }
}
