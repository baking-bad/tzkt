﻿namespace Tzkt.Api.Models
{
    public class NonceRevelationOperation : Operation
    {
        /// <summary>
        /// Type of the operation, `nonce_revelation` - are used by the blockchain to create randomness
        /// </summary>
        public override string Type => ActivityTypes.NonceRevelation;

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
        public required string Block { get; set; }

        /// <summary>
        /// Hash of the operation
        /// </summary>
        public required string Hash { get; set; }

        /// <summary>
        /// Information about the delegate (baker), who produced the block with the operation
        /// </summary>
        public required Alias Baker { get; set; }

        /// <summary>
        /// Information about the delegate (baker), who revealed the nonce (sent the operation)
        /// </summary>
        public required Alias Sender { get; set; }

        /// <summary>
        /// Block height of the block, where seed nonce hash is stored
        /// </summary>
        public int RevealedLevel { get; set; }

        /// <summary>
        /// Cycle for which seed nonce was revealed
        /// </summary>
        public int RevealedCycle { get; set; }

        /// <summary>
        /// Seed nonce hex
        /// </summary>
        public required string Nonce { get; set; }

        /// <summary>
        /// Reward, corresponding to delegated stake, paid to baker's liquid balance (micro tez)
        /// (it is not frozen and can be spent immediately).
        /// </summary>
        public long RewardDelegated { get; set; }

        /// <summary>
        /// Reward, corresponding to baker's own stake, paid to baker's own staked balance (micro tez)
        /// (it is frozen and belongs to the baker).
        /// </summary>
        public long RewardStakedOwn { get; set; }

        /// <summary>
        /// Reward, corresponding to baker's edge from external stake, paid to baker's own staked balance (micro tez)
        /// (it is frozen and belongs to the baker).
        /// </summary>
        public long RewardStakedEdge { get; set; }

        /// <summary>
        /// Reward, corresponding to baker's external stake, paid to baker's external staked balance (micro tez)
        /// (it is frozen and belongs to baker's stakers).
        /// </summary>
        public long RewardStakedShared { get; set; }

        #region injecting
        /// <summary>
        /// Injected historical quote at the time of operation
        /// </summary>
        public QuoteShort? Quote { get; set; }
        #endregion
    }
}
