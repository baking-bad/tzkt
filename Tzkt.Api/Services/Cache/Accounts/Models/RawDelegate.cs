namespace Tzkt.Api.Services.Cache
{
    public class RawDelegate : RawUser
    {
        public int ActivationLevel { get; set; }
        public int DeactivationLevel { get; set; }

        public long? FrozenDepositLimit { get; set; }
        public long FrozenDeposit { get; set; }

        public long StakingBalance { get; set; }
        public long DelegatedBalance { get; set; }
        public int DelegatorsCount { get; set; }

        public int BlocksCount { get; set; }
        public int EndorsementsCount { get; set; }
        public int PreendorsementsCount { get; set; }
        public int BallotsCount { get; set; }
        public int ProposalsCount { get; set; }
        public int DoubleBakingCount { get; set; }
        public int DoubleEndorsingCount { get; set; }
        public int DoublePreendorsingCount { get; set; }
        public int NonceRevelationsCount { get; set; }
        public int VdfRevelationsCount { get; set; }
        public int RevelationPenaltiesCount { get; set; }
        public int EndorsingRewardsCount { get; set; }

        public int? SoftwareId { get; set; }
    }
}
