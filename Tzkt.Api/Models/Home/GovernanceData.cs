using System;

namespace Tzkt.Api.Models.Home
{
    public class GovernanceData
    {
        public string Protocol { get; set; }
        public string Proposal { get; set; }
        
        public string Period { get; set; }
        public DateTime PeriodEndTime { get; set; }
        public DateTime EpochEndTime { get; set; }
        
        public double? Supermajority { get; set; }
        public double? YayVotes { get; set; }
        
        public double? Quorum { get; set; }
        public double? Participation { get; set; }
    }
}