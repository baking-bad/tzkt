namespace Tzkt.Api.Models
{
    public class StakerRewards
    {
        /// <summary>
        /// Cycle in which the rewards were earned.  
        /// **[sortable]**
        /// </summary>
        public int Cycle { get; set; }

        /// <summary>
        /// Staker's baker.
        /// </summary>
        public required Alias Baker { get; set; }

        /// <summary>
        /// Staked balance at the beginning of the cycle (micro tez).
        /// </summary>
        public long InitialStake { get; set; }

        /// <summary>
        /// Amount added (staked) during the cycle (micro tez).
        /// </summary>
        public long AddedStake { get; set; }

        /// <summary>
        /// Amount removed (unstaked) during the cycle (micro tez).
        /// </summary>
        public long RemovedStake { get; set; }

        /// <summary>
        /// Staked balance at the end of the cycle (micro tez).
        /// </summary>
        public long FinalStake { get; set; }

        /// <summary>
        /// Average (per-block) staked balance (micro tez).
        /// </summary>
        public long AvgStake { get; set; }

        /// <summary>
        /// Staking rewards (or losses if negative) earned during the cycle (micro tez).
        /// </summary>
        public long Rewards { get; set; }

        /// <summary>
        /// Rewards of the staker's baker, from which the staker can understand the source of the rewards/losses.
        /// </summary>
        public required BakerRewards BakerRewards { get; set; }

        #region injecting
        /// <summary>
        /// Injected historical quote at the end of the cycle
        /// </summary>
        public QuoteShort? Quote { get; set; }
        #endregion
    }
}
