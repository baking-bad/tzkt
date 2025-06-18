namespace Tzkt.Api.Models
{
    public class AttestationRewardOperation : Operation
    {
        /// <summary>
        /// Type of the operation, `attestation_reward`
        /// </summary>
        public override string Type => ActivityTypes.AttestationReward;

        /// <summary>
        /// Unique ID of the operation, stored in the TzKT indexer database
        /// </summary>
        public override long Id { get; set; }

        /// <summary>
        /// Height of the block from the genesis
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// Datetime at which the block is claimed to have been created (ISO 8601, e.g. `2020-02-20T02:40:57Z`)
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Block hash
        /// </summary>
        public required string Block { get; set; }

        /// <summary>
        /// Baker expected to receive attestation reward
        /// </summary>
        public required Alias Baker { get; set; }

        /// <summary>
        /// Expected attestation reward, based on baker's active stake (micro tez)
        /// </summary>
        public long Expected { get; set; }

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
