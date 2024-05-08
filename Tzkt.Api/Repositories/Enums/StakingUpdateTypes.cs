using Tzkt.Data.Models;

namespace Tzkt.Api
{
    static class StakingUpdateTypes
    {
        public const string Stake = "stake";
        public const string Unstake = "unstake";
        public const string Restake = "restake";
        public const string Finalize = "finalize";
        public const string SlashStaked = "slash_staked";
        public const string SlashUnstaked = "slash_unstaked";

        public static bool TryParse(string value, out int res)
        {
            res = value switch
            {
                Stake => (int)StakingUpdateType.Stake,
                Unstake => (int)StakingUpdateType.Unstake,
                Restake => (int)StakingUpdateType.Restake,
                Finalize => (int)StakingUpdateType.Finalize,
                SlashStaked => (int)StakingUpdateType.SlashStaked,
                SlashUnstaked => (int)StakingUpdateType.SlashUnstaked,
                _ => -1
            };
            return res != -1;
        }

        public static string ToString(int value) => value switch
        {
            (int)StakingUpdateType.Stake => Stake,
            (int)StakingUpdateType.Unstake => Unstake,
            (int)StakingUpdateType.Restake => Restake,
            (int)StakingUpdateType.Finalize => Finalize,
            (int)StakingUpdateType.SlashStaked => SlashStaked,
            (int)StakingUpdateType.SlashUnstaked => SlashUnstaked,
            _ => throw new Exception("invalid staking action value")
        };
    }
}
