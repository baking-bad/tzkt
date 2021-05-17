using System;
using System.Collections.Generic;

namespace Tzkt.Api.Models
{
    public class GovernanceData
    {
        public string Protocol { get; set; }
        public string Proposal { get; set; }
        
        public string Period { get; set; }
        public DateTime PeriodEndTime { get; set; }
        public DateTime EpochEndTime { get; set; }

        #region proposal
        
        public List<ProposalData> Proposals { get; set; }
        public double? UpvotesQuorum { get; set; }

        #endregion

        
        public double? Supermajority { get; set; }
        public double? YayVotes { get; set; }
        
        public double? Quorum { get; set; }
        public double? Participation { get; set; }
    }
}