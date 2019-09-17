using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

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

    public static class TestingPeriodModel
    {
        public static void BuildTestingPeriodModel(this ModelBuilder modelBuilder)
        {

        }
    }
}
