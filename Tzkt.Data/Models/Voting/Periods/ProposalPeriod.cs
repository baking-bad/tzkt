using System.Collections.Generic;

namespace Tzkt.Data.Models
{
    public class ProposalPeriod : VotingPeriod
    {
        #region relations
        public List<Proposal> Candidates { get; set; }
        #endregion
    }
}
