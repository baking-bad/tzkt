using System;

namespace Tzkt.Api.Models
{
    public class EndorsingRewardOperation : Operation
    {
        /// <summary>
        /// Type of the operation, `endorsing_reward`
        /// </summary>
        public override string Type => OpTypes.EndorsingReward;

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
        public string Block { get; set; }

        /// <summary>
        /// Baker expected to receive endorsing reward
        /// </summary>
        public Alias Baker { get; set; }

        /// <summary>
        /// Expected endorsing reward, based on baker's active stake (micro tez)
        /// </summary>
        public long Expected { get; set; }

        /// <summary>
        /// Actually received endorsing reward (micro tez)
        /// </summary>
        public long Received { get; set; }

        #region injecting
        /// <summary>
        /// Injected historical quote at the time of operation
        /// </summary>
        public QuoteShort Quote { get; set; }
        #endregion
    }
}
