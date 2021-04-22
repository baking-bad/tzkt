using System;

namespace Tzkt.Api.Models.Home
{
    public class GovernanceData
    {
        public string Proposal { get; set; }
        public string CurrentPeriod { get; set; }
        public DateTime PeriodEnds { get; set; }
        public DateTime ProtocolWillBeApplied { get; set; }
        public string Hash { get; set; }
        public double? Supermajority { get; set; }
        public double? InFavor { get; set; }
        
        public double? Quorum { get; set; }
        public double? Participation { get; set; }
    }
}