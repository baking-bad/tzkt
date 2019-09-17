using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class ProposalPeriod : VotingPeriod
    {
        #region indirect relations
        public List<Proposal> Candidates { get; set; }
        #endregion
    }

    public static class ProposalPeriodModel
    {
        public static void BuildProposalPeriodModel(this ModelBuilder modelBuilder)
        {

        }
    }
}
