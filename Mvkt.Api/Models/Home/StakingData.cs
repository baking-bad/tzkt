namespace Mvkt.Api.Models
{
    public class StakingData
    {
        public long TotalStaking { get; set; }
        public double StakingPercentage { get; set; }
        public double AvgRoi { get; set; }
        public double Inflation { get; set; }
        public int Bakers { get; set; }
        public int FundedBakers { get; set; }

        public long OwnStaked { get; set; }
        public double OwnStakedPercentage { get; set; }

        public long ExternalStaked { get; set; }
        public double ExternalStakedPercentage { get; set; }

        public long TotalStaked { get; set; }
        public double TotalStakedPercentage { get; set; }

        public long OwnDelegated { get; set; }
        public double OwnDelegatedPercentage { get; set; }

        public long ExternalDelegated { get; set; }
        public double ExternalDelegatedPercentage { get; set; }

        public long TotalDelegated { get; set; }
        public double TotalDelegatedPercentage { get; set; }

        public double StakingApy { get; set; }
        public double DelegationApy { get; set; }
    }
}