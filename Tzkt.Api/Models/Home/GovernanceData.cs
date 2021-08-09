using System;
using System.Collections.Generic;

namespace Tzkt.Api.Models
{
    public class GovernanceData
    {
        public string Protocol { get; set; }
        public string Proposal { get; set; }
        
        public int Epoch { get; set; }
        public string Period { get; set; }
        public DateTime PeriodEndTime { get; set; }
        public DateTime EpochStartTime { get; set; }
        public DateTime EpochEndTime { get; set; }

        public List<ProposalData> Proposals { get; set; }
        public double? UpvotesQuorum { get; set; }
        
        public double? BallotsQuorum { get; set; }
        public double? Participation { get; set; }

        public double? Supermajority { get; set; }
        public double? YayVotes { get; set; }
    }
}