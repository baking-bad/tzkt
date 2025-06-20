﻿namespace Tzkt.Api.Models
{
    public class DoubleEndorsingOperation : Operation
    {
        /// <summary>
        /// Type of the operation, `double_endorsing` - is used by bakers to provide evidence of double endorsement
        /// (endorsing two different blocks at the same block height) by a baker
        /// </summary>
        public override string Type => ActivityTypes.DoubleEndorsing;

        /// <summary>
        /// Unique ID of the operation, stored in the TzKT indexer database
        /// </summary>
        public override long Id { get; set; }

        /// <summary>
        /// Height of the block from the genesis block, in which the operation was included
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
        /// Height of the block from the genesis, at which double endorsing occurred 
        /// </summary>
        public int AccusedLevel { get; set; }

        /// <summary>
        /// Height of the block from the genesis, at which the offender was slashed
        /// </summary>
        public int SlashedLevel { get; set; }

        /// <summary>
        /// Information about the baker, produced the block, in which the accusation was included
        /// </summary>
        public required Alias Accuser { get; set; }

        /// <summary>
        /// Reward of the baker, produced the block, in which the accusation was included
        /// </summary>
        public long Reward { get; set; }

        /// <summary>
        /// Information about the baker, accused for producing two different endorsements at the same level
        /// </summary>
        public required Alias Offender { get; set; }

        /// <summary>
        /// Amount slashed from baker's own staked balance
        /// </summary>
        public long LostStaked { get; set; }

        /// <summary>
        /// Amount slashed from baker's own unstaked balance
        /// </summary>
        public long LostUnstaked { get; set; }

        /// <summary>
        /// Amount slashed from baker's external staked balance
        /// </summary>
        public long LostExternalStaked { get; set; }

        /// <summary>
        /// Amount slashed from baker's external unstaked balance
        /// </summary>
        public long LostExternalUnstaked { get; set; }

        /// <summary>
        /// Number of staking updates happened internally
        /// </summary>
        public int? StakingUpdatesCount { get; set; }

        #region injecting
        /// <summary>
        /// Injected historical quote at the time of operation
        /// </summary>
        public QuoteShort? Quote { get; set; }
        #endregion
    }
}
