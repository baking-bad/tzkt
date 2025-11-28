namespace Mvkt.Api.Models
{
    /// <summary>
    /// Baker statistics based on historical rewards
    /// </summary>
    public class BakerStats
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
        /// Current APY metrics for the baker
        /// </summary>
        public BakerApy Apy { get; set; }

        /// <summary>
        /// Luck percentage (actual rewards / expected rewards * 100)
        /// Values &gt; 100% indicate good luck, &lt; 100% indicate bad luck
        /// </summary>
        public double Luck { get; set; }

        /// <summary>
        /// Performance percentage (successful operations / total opportunities * 100)
        /// </summary>
        public double Performance { get; set; }

        /// <summary>
        /// Reliability percentage (blocks + endorsements / expected blocks + expected endorsements * 100)
        /// </summary>
        public double Reliability { get; set; }

        /// <summary>
        /// Total expected rewards (micro tez)
        /// </summary>
        public long TotalExpectedRewards { get; set; }

        /// <summary>
        /// Total actual rewards received (micro tez)
        /// </summary>
        public long TotalActualRewards { get; set; }
    }
}

