using System.Numerics;

namespace Tzkt.Api.Services.Cache
{
    public class RawDelegate : RawUser
    {
        public int ActivationLevel { get; set; }
        public int DeactivationLevel { get; set; }

        public string? ConsensusAddress { get; set; }
        public string? CompanionAddress { get; set; }

        public long BakingPower { get; set; }
        public long VotingPower { get; set; }

        public long OwnDelegatedBalance { get; set; }
        public long ExternalDelegatedBalance { get; set; }
        public int DelegatorsCount { get; set; }

        public long OwnStakedBalance { get; set; }
        public long ExternalStakedBalance { get; set; }
        public BigInteger? IssuedPseudotokens { get; set; }
        public int StakersCount { get; set; }

        public long ExternalUnstakedBalance { get; set; }
        public long RoundingError { get; set; }

        public long? FrozenDepositLimit { get; set; }
        public long? LimitOfStakingOverBaking { get; set; }
        public long? EdgeOfBakingOverStaking { get; set; }

        public int BlocksCount { get; set; }
        public int AttestationsCount { get; set; }
        public int PreattestationsCount { get; set; }
        public int BallotsCount { get; set; }
        public int ProposalsCount { get; set; }
        public int DalEntrapmentEvidenceOpsCount { get; set; }
        public int DoubleBakingCount { get; set; }
        public int DoubleConsensusCount { get; set; }
        public int NonceRevelationsCount { get; set; }
        public int VdfRevelationsCount { get; set; }
        public int RevelationPenaltiesCount { get; set; }
        public int AttestationRewardsCount { get; set; }
        public int DalAttestationRewardsCount { get; set; }
        public int AutostakingOpsCount { get; set; }

        public int? SoftwareId { get; set; }
        public int? SoftwareUpdateLevel { get; set; }
    }
}
