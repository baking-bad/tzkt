using Tzkt.Data.Models;

namespace Tzkt.Api
{
    static class AutostakingActions
    {
        public const string Stake = "stake";
        public const string Unstake = "unstake";
        public const string Finalize = "finalize";
        public const string Restake = "restake";

        public static bool TryParse(string value, out int res)
        {
            res = value switch
            {
                Stake => (int)AutostakingAction.Stake,
                Unstake => (int)AutostakingAction.Unstake,
                Finalize => (int)AutostakingAction.Finalize,
                Restake => (int)AutostakingAction.Restake,
                _ => -1
            };
            return res != -1;
        }

        public static string ToString(int value) => value switch
        {
            (int)AutostakingAction.Stake => Stake,
            (int)AutostakingAction.Unstake => Unstake,
            (int)AutostakingAction.Finalize => Finalize,
            (int)AutostakingAction.Restake => Restake,
            _ => throw new Exception("invalid autostaking action value")
        };
    }
}
