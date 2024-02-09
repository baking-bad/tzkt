namespace Mvkt.Api.Services.Cache
{
    public class RawUser : RawAccount
    {
        public bool Revealed { get; set; }
        public string PublicKey { get; set; }

        public long StakedBalance { get; set; }
        public long StakedPseudotokens { get; set; }
        public long UnstakedBalance { get; set; }
        public int? UnstakedBakerId { get; set; }

        public bool? Activated { get; set; }
        public int RegisterConstantsCount { get; set; }
        public int SetDepositsLimitsCount { get; set; }
        public int StakingOpsCount { get; set; }
    }
}
