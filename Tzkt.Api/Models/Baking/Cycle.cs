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
        /// Level of the first block in the cycle
        /// </summary>
        public int FirstLevel { get; set; }

        /// <summary>
        /// Timestamp of the first block in the cycle
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Level of the last block in the cycle
        /// </summary>
        public int LastLevel { get; set; }

        /// <summary>
        /// Timestamp of the last block in the cycle
        /// </summary>
        public DateTime EndTime { get; set; }

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

        /// <summary>
        /// Total number of bakers in stake distribution for the cycle
        /// </summary>
        public int SelectedBakers { get; set; }

        /// <summary>
        /// Total stake of bakers in stake distribution for the cycle
        /// </summary>
        public long SelectedStake { get; set; }

        #region injecting
        /// <summary>
        /// Injected historical quote at the end of the cycle
        /// </summary>
        public QuoteShort Quote { get; set; }
        #endregion

        #region deprecated
        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public int TotalRolls => 0;
        #endregion
    }
}
