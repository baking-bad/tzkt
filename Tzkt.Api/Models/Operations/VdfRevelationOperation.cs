namespace Tzkt.Api.Models
{
    public class VdfRevelationOperation : Operation
    {
        /// <summary>
        /// Type of the operation, `vdf_revelation` - used by the blockchain to create randomness
        /// </summary>
        public override string Type => ActivityTypes.VdfRevelation;

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
        /// Cycle in which the operation was included
        /// </summary>
        public int Cycle { get; set; }

        /// <summary>
        /// Vdf solution
        /// </summary>
        public required string Solution { get; set; }

        /// <summary>
        /// Vdf proof
        /// </summary>
        public required string Proof { get; set; }

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
