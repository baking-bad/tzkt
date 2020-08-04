using System;

namespace Tzkt.Api.Models
{
    public class Cycle
    {
        /// <summary>
        /// Cycle index starting from zero
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Index of the snapshot
        /// </summary>
        public int SnapshotIndex { get; set; }

        /// <summary>
        /// Height of the block where the snapshot was taken
        /// </summary>
        public int SnapshotLevel { get; set; }

        /// <summary>
        /// Randomly generated seed used by the network for things like baking rights distribution etc.
        /// </summary>
        public string RandomSeed { get; set; }

        /// <summary>
        /// Total number of all active in this cycle bakers
        /// </summary>
        public int TotalBakers { get; set; }

        /// <summary>
        /// Total number of rolls involved in baking rights distribution
        /// </summary>
        public int TotalRolls { get; set; }

        /// <summary>
        /// Total staking balance of all active in this cycle bakers
        /// </summary>
        public long TotalStaking { get; set; }

        /// <summary>
        /// Total number of active bakers' delegators
        /// </summary>
        public int TotalDelegators { get; set; }

        /// <summary>
        /// Total balance delegated to active bakers
        /// </summary>
        public long TotalDelegated { get; set; }

        #region injecting
        /// <summary>
        /// Injected historical quote at the end of the cycle
        /// </summary>
        public QuoteShort Quote { get; set; }
        #endregion
    }
}
