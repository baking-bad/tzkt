﻿namespace Tzkt.Api.Models
{
    public class BakingOperation : Operation
    {
        /// <summary>
        /// Type of the operation, `baking` - an operation which contains brief information about a baked (produced) block (synthetic type)
        /// </summary>
        public override string Type => ActivityTypes.Baking;

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
        /// Baker who proposed the block payload
        /// </summary>
        public required Alias Proposer { get; set; }

        /// <summary>
        /// Baker who produced the block
        /// </summary>
        public required Alias Producer { get; set; }

        /// <summary>
        /// Round at which the block payload was proposed
        /// </summary>
        public int PayloadRound { get; set; }

        /// <summary>
        /// Round at which the block was produced
        /// </summary>
        public int BlockRound { get; set; }

        /// <summary>
        /// Security deposit frozen on the baker's account for producing the block (micro tez)
        /// </summary>
        public long Deposit { get; set; }

        /// <summary>
        /// Portion of fixed reward, corresponding to delegated stake, paid to payload proposer's liquid balance (micro tez)
        /// (it is not frozen and can be spent immediately).
        /// </summary>
        public long RewardDelegated { get; set; }

        /// <summary>
        /// Portion of fixed reward, corresponding to baker's own stake, paid to payload proposer's own staked balance (micro tez)
        /// (it is frozen and belongs to the baker).
        /// </summary>
        public long RewardStakedOwn { get; set; }

        /// <summary>
        /// Portion of fixed reward, corresponding to baker's edge from external stake, paid to payload proposer's own staked balance (micro tez)
        /// (it is frozen and belongs to the baker).
        /// </summary>
        public long RewardStakedEdge { get; set; }

        /// <summary>
        /// Portion of fixed reward, corresponding to baker's external stake, paid to payload proposer's external staked balance (micro tez)
        /// (it is frozen and belongs to baker's stakers).
        /// </summary>
        public long RewardStakedShared { get; set; }

        /// <summary>
        /// Portion of bonus reward, corresponding to delegated stake, paid to block producer's liquid balance (micro tez)
        /// (it is not frozen and can be spent immediately).
        /// </summary>
        public long BonusDelegated { get; set; }

        /// <summary>
        /// Portion of bonus reward, corresponding to baker's own stake, paid to block producer's own staked balance (micro tez)
        /// (it is frozen and belongs to the baker).
        /// </summary>
        public long BonusStakedOwn { get; set; }

        /// <summary>
        /// Portion of bonus reward, corresponding to baker's edge from external stake, paid to block producer's own staked balance (micro tez)
        /// (it is frozen and belongs to the baker).
        /// </summary>
        public long BonusStakedEdge { get; set; }

        /// <summary>
        /// Portion of fixed reward, corresponding to baker's external stake, paid to block producer's external staked balance (micro tez)
        /// (it is frozen and belongs to baker's stakers).
        /// </summary>
        public long BonusStakedShared { get; set; }

        /// <summary>
        /// Total fee gathered from operations, included into the block
        /// </summary>
        public long Fees { get; set; }

        #region injecting
        /// <summary>
        /// Injected historical quote at the time of operation
        /// </summary>
        public QuoteShort? Quote { get; set; }
        #endregion
    }
}
