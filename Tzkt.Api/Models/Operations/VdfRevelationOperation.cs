﻿namespace Tzkt.Api.Models
{
    public class VdfRevelationOperation : Operation
    {
        /// <summary>
        /// Type of the operation, `vdf_revelation` - used by the blockchain to create randomness
        /// </summary>
        public override string Type => OpTypes.VdfRevelation;

        /// <summary>
        /// Unique ID of the operation, stored in the MvKT indexer database
        /// </summary>
        public override long Id { get; set; }

        /// <summary>
        /// The height of the block from the genesis block, in which the operation was included
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// Datetime of the block, in which the operation was included (ISO 8601, e.g. `2020-02-20T02:40:57Z`)
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Hash of the block, in which the operation was included
        /// </summary>
        public string Block { get; set; }

        /// <summary>
        /// Hash of the operation
        /// </summary>
        public string Hash { get; set; }

        /// <summary>
        /// Information about the delegate (baker), who produced the block with the operation
        /// </summary>
        public Alias Baker { get; set; }

        /// <summary>
        /// Cycle in which the operation was included
        /// </summary>
        public int Cycle { get; set; }

        /// <summary>
        /// Vdf solution
        /// </summary>
        public string Solution { get; set; }

        /// <summary>
        /// Vdf proof
        /// </summary>
        public string Proof { get; set; }

        /// <summary>
        /// Reward received on baker's liquid balance (micro tez)
        /// </summary>
        public long RewardLiquid { get; set; }

        /// <summary>
        /// Reward received on baker's staked balance (micro tez)
        /// </summary>
        public long RewardStakedOwn { get; set; }

        /// <summary>
        /// Reward received on baker's external staked balance (micro tez)
        /// </summary>
        public long RewardStakedShared { get; set; }

        #region injecting
        /// <summary>
        /// Injected historical quote at the time of operation
        /// </summary>
        public QuoteShort Quote { get; set; }
        #endregion

        #region deprecated
        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long Reward => RewardLiquid + RewardStakedOwn + RewardStakedShared;
        #endregion
    }
}
