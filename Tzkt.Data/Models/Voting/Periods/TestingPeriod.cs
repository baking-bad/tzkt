using System.ComponentModel.DataAnnotations.Schema;

namespace Tzkt.Data.Models
{ 
    public class TestingPeriod : VotingPeriod
    {
        public int ProposalId { get; set; }

        #region relations
        [ForeignKey(nameof(ProposalId))]
        public Proposal Proposal { get; set; }
        #endregion
    }
}
