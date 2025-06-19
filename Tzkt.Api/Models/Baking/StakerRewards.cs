namespace Tzkt.Api.Models
{
    public class StakerRewards
    {
        /// <summary>
        /// Internal TzKT ID.  
        /// **[sortable]**
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Cycle in which the rewards were earned.  
        /// **[sortable]**
        /// </summary>
        public int Cycle { get; set; }

        /// <summary>
        /// Staker.
        /// </summary>
        public required Alias Staker { get; set; }

        /// <summary>
        /// Staker's baker.
        /// </summary>
        public required Alias Baker { get; set; }

        /// <summary>
        /// Edge of baking over staking parameter the baker had in this cycle.
        /// </summary>
        public long EdgeOfBakingOverStaking { get; set; }

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
        /// Staking rewards earned during the cycle (micro tez).
        /// </summary>
        public long Rewards { get; set; }

        #region injecting
        /// <summary>
        /// Injected historical quote at the end of the cycle
        /// </summary>
        public QuoteShort? Quote { get; set; }
        #endregion
    }
}
