using System.ComponentModel.DataAnnotations.Schema;
using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    public class ProposalOperation : BaseOperation
    {
        public int PeriodId { get; set; }
        public int ProposalId { get; set; }
        public int SenderId { get; set; }

        #region relations
        [ForeignKey(nameof(PeriodId))]
        public VotingPeriod Period { get; set; }

        [ForeignKey(nameof(ProposalId))]
        public Proposal Proposal { get; set; }

        [ForeignKey(nameof(SenderId))]
        public Delegate Sender { get; set; }
        #endregion
    }
}
