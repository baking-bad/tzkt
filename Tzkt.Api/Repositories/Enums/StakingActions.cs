using Tzkt.Data.Models;

namespace Tzkt.Api
{
    static class StakingActions
    {
        public const string Stake = "stake";
        public const string Unstake = "unstake";
        public const string Finalize = "finalize";

        public static bool TryParse(string value, out int res)
        {
            res = value switch
            {
                Stake => (int)StakingAction.Stake,
                Unstake => (int)StakingAction.Unstake,
                Finalize => (int)StakingAction.Finalize,
                _ => -1
            };
            return res != -1;
        }

        public static string ToString(int value) => value switch
        {
            (int)StakingAction.Stake => Stake,
            (int)StakingAction.Unstake => Unstake,
            (int)StakingAction.Finalize => Finalize,
            _ => throw new Exception("invalid staking action value")
        };
    }
}
