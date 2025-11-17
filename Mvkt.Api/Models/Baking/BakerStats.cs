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
        /// Total frozen bonds (own staked + external staked) (micro tez)
        /// </summary>
        public long Bonds { get; set; }

        /// <summary>
        /// Total income from all sources (micro tez)
        /// </summary>
        public long TotalIncome { get; set; }

        /// <summary>
        /// Total fees collected from blocks (micro tez)
        /// </summary>
        public long Fees { get; set; }

        /// <summary>
        /// Baker's own rewards (excluding rewards to stakers/delegators) (micro tez)
        /// </summary>
        public long BakerRewards { get; set; }

        /// <summary>
        /// Extra rewards (double baking/endorsing, revelations) (micro tez)
        /// </summary>
        public long ExtraRewards { get; set; }

        /// <summary>
        /// Lost rewards due to missed blocks/endorsements (micro tez)
        /// </summary>
        public long LostRewards { get; set; }

        /// <summary>
        /// Slashed rewards due to double baking/endorsing/preendorsing (micro tez)
        /// </summary>
        public long SlashedRewards { get; set; }

        /// <summary>
        /// Luck percentage (actual rewards / expected rewards * 100)
        /// Values > 100% indicate good luck, < 100% indicate bad luck
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
        /// Total number of cycles analyzed
        /// </summary>
        public int CyclesCount { get; set; }

        /// <summary>
        /// Total expected blocks
        /// </summary>
        public double TotalExpectedBlocks { get; set; }

        /// <summary>
        /// Total actual blocks baked
        /// </summary>
        public int TotalBlocks { get; set; }

        /// <summary>
        /// Total missed blocks
        /// </summary>
        public int TotalMissedBlocks { get; set; }

        /// <summary>
        /// Total expected endorsements
        /// </summary>
        public double TotalExpectedEndorsements { get; set; }

        /// <summary>
        /// Total actual endorsements
        /// </summary>
        public int TotalEndorsements { get; set; }

        /// <summary>
        /// Total missed endorsements
        /// </summary>
        public int TotalMissedEndorsements { get; set; }

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

