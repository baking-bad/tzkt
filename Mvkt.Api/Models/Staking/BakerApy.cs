namespace Mvkt.Api.Models
{
    /// <summary>
    /// Baker APY information (only yield percentages)
    /// </summary>
    public class BakerApy
    {
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
