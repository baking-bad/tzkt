using Tzkt.Data.Models;

namespace Tzkt.Api
{
    static class StakingOperationKinds
    {
        public const string Stake = "stake";
        public const string Unstake = "unstake";
        public const string Finalize = "finalize";
        public const string SetParameters = "set_parameters";

        public static bool TryParse(string value, out int res)
        {
            res = value switch
            {
                Stake => (int)StakingOperationKind.Stake,
                Unstake => (int)StakingOperationKind.Unstake,
                Finalize => (int)StakingOperationKind.FinalizeUnstake,
                SetParameters => (int)StakingOperationKind.SetDelegateParameter,
                _ => -1
            };
            return res != -1;
        }

        public static string ToString(int value) => value switch
        {
            (int)StakingOperationKind.Stake => Stake,
            (int)StakingOperationKind.Unstake => Unstake,
            (int)StakingOperationKind.FinalizeUnstake => Finalize,
            (int)StakingOperationKind.SetDelegateParameter => SetParameters,
            _ => throw new Exception("invalid staking operation kind value")
        };
    }
}
