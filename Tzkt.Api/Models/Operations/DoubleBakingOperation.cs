using System;

namespace Tzkt.Api.Models
{
    public class DoubleBakingOperation : Operation
    {
        /// <summary>
        /// Type of the operation, `double_baking` - is used by bakers to provide evidence of double baking
        /// (baking two different blocks at the same height) by a baker
        /// </summary>
        public override string Type => OpTypes.DoubleBaking;

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
        public string Block { get; set; }

        /// <summary>
        /// Hash of the operation
        /// </summary>
        public string Hash { get; set; }

        /// <summary>
        /// Height of the block from the genesis, which was double baked
        /// </summary>
        public int AccusedLevel { get; set; }

        /// <summary>
        /// Information about the baker, produced the block, in which the accusation was included
        /// </summary>
        public Alias Accuser { get; set; }

        /// <summary>
        /// Reward of the baker, produced the block, in which the accusation was included
        /// </summary>
        public long AccuserReward { get; set; }

        /// <summary>
        /// Information about the baker, accused for producing two different blocks at the same level
        /// </summary>
        public Alias Offender { get; set; }

        /// <summary>
        /// Amount of frozen deposits lost by accused baker
        /// </summary>
        public long OffenderLoss { get; set; }

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
        public long AccuserRewards => AccuserReward;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long OffenderLostDeposits => OffenderLoss;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long OffenderLostRewards => 0;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long OffenderLostFees => 0;
        #endregion
    }
}
