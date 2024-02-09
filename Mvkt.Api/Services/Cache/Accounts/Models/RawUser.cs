using System.Numerics;

namespace Mvkt.Api.Services.Cache
{
    public class RawUser : RawAccount
    {
        public bool Revealed { get; set; }
        public string PublicKey { get; set; }

        public BigInteger? StakedPseudotokens { get; set; }
        public long UnstakedBalance { get; set; }
        public int? UnstakedBakerId { get; set; }

        public int? StakingUpdatesCount { get; set; }

        public int ActivationsCount { get; set; }
        public int RegisterConstantsCount { get; set; }
        public int SetDepositsLimitsCount { get; set; }
        public int StakingOpsCount { get; set; }
        public int SetDelegateParametersOpsCount { get; set; }
        public int DalPublishCommitmentOpsCount { get; set; }
    }
}
