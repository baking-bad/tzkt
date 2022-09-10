using System;

namespace Tzkt.Api.Models
{
    public class RevelationPenaltyOperation : Operation
    {
        /// <summary>
        /// Type of the operation, `revelation_penalty` - is operation, in which rewards were lost due to unrevealed seed nonces by the delegate (synthetic type)
        /// </summary>
        public override string Type => OpTypes.RevelationPenalty;

        /// <summary>
        /// Unique ID of the operation, stored in the TzKT indexer database
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
        /// Information about the delegate (baker) who has lost rewards due to unrevealed seed nonces 
        /// </summary>
        public Alias Baker { get; set; }

        /// <summary>
        /// Height of the block, which contains hash of the seed nonce, which was to be revealed
        /// </summary>
        public int MissedLevel { get; set; }

        /// <summary>
        /// Reward for baking and gathered fees from the block, which were lost due to unrevealed seed nonces (micro tez)
        /// </summary>
        public long Loss { get; set; }

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
        public long LostReward => Loss;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long LostFees => 0;
        #endregion
    }
}
