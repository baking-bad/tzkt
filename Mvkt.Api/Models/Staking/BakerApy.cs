namespace Mvkt.Api.Models
{
    /// <summary>
    /// Baker APY information
    /// </summary>
    public class BakerApy
    {
        /// <summary>
        /// Baker address
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// Baker alias
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// Baker's own staked balance (micro tez)
        /// </summary>
        public long OwnStakedBalance { get; set; }

        /// <summary>
        /// External staked balance (micro tez)
        /// </summary>
        public long ExternalStakedBalance { get; set; }

        /// <summary>
        /// Total delegated balance (micro tez)
        /// </summary>
        public long DelegatedBalance { get; set; }

        /// <summary>
        /// Baker's effective stake (2 * own_staked + external_staked + delegated / stake_power_multiplier)
        /// </summary>
        public long EffectiveStake { get; set; }

        /// <summary>
        /// Network-wide total effective stake
        /// </summary>
        public long TotalEffectiveStake { get; set; }

        /// <summary>
        /// Network-wide total monthly rewards (micro tez)
        /// </summary>
        public long TotalMonthlyRewards { get; set; }

        /// <summary>
        /// Baker's expected monthly rewards (micro tez)
        /// </summary>
        public long ExpectedMonthlyRewards { get; set; }

        /// <summary>
        /// APY for baker's own stake (percentage)
        /// </summary>
        public double OwnStakeApy { get; set; }

        /// <summary>
        /// APY for external stakers (percentage)
        /// </summary>
        public double ExternalStakeApy { get; set; }

        /// <summary>
        /// APY for delegators (percentage)
        /// </summary>
        public double DelegationApy { get; set; }
    }
}

