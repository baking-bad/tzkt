﻿namespace Tzkt.Api.Models
{
    public class UnstakeRequest
    {
        /// <summary>
        /// Internal TzKT ID.  
        /// **[sortable]**
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Cycle at which the unstake request was created.  
        /// **[sortable]**
        /// </summary>
        public int Cycle { get; set; }

        /// <summary>
        /// Related baker.
        /// </summary>
        public required Alias Baker { get; set; }

        /// <summary>
        /// Related staker.
        /// </summary>
        public Alias? Staker { get; set; }

        /// <summary>
        /// Initially requested amount (mutez).
        /// </summary>
        public long RequestedAmount { get; set; }

        /// <summary>
        /// Amount that was restaked back (mutez).
        /// </summary>
        public long RestakedAmount { get; set; }

        /// <summary>
        /// Finalized amount (mutez).
        /// </summary>
        public long FinalizedAmount { get; set; }

        /// <summary>
        /// Slashed amount (mutez).
        /// </summary>
        public long SlashedAmount { get; set; }

        /// <summary>
        /// Protocol rounding error, appearing after slashing.
        /// </summary>
        public long? RoundingError { get; set; }

        /// <summary>
        /// Actual amount that was/is/will be available for finalizing. 
        /// </summary>
        public long ActualAmount { get; set; }

        /// <summary>
        /// Status of the unstake request (`pending`, `finalizable`, `finalized`).
        /// </summary>
        public required string Status { get; set; }

        /// <summary>
        /// Number of staking updates related to the unstake request.
        /// </summary>
        public int UpdatesCount { get; set; }

        /// <summary>
        /// Level of the block where the unstake request was created.  
        /// **[sortable]**
        /// </summary>
        public int FirstLevel { get; set; }

        /// <summary>
        /// Timestamp of the block where the unstake request was created.
        /// </summary>
        public DateTime FirstTime { get; set; }

        /// <summary>
        /// Level of the block where the unstake request was last updated.  
        /// **[sortable]**
        /// </summary>
        public int LastLevel { get; set; }

        /// <summary>
        /// Timestamp of the block where the unstake request was last updated.
        /// </summary>
        public DateTime LastTime { get; set; }
    }
}
