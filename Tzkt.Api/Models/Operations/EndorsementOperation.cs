using System;

namespace Tzkt.Api.Models
{
    public class EndorsementOperation : Operation
    {
        /// <summary>
        /// Type of the operation, `endorsement` - is operation, which specifies the head of the chain as seen by the endorser of a given slot.
        /// The endorser is randomly selected to be included in the block that extends the head of the chain as specified in this operation.
        /// A block with more endorsements improves the weight of the chain and increases the likelihood of that chain being the canonical one.
        /// </summary>
        public override string Type => OpTypes.Endorsement;

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
        /// Hash of the operation
        /// </summary>
        public string Hash { get; set; }

        /// <summary>
        /// Information about the baker who sent the operation
        /// </summary>
        public Alias Delegate { get; set; }

        /// <summary>
        /// Number of assigned endorsement slots to the baker who sent the operation
        /// </summary>
        public int Slots { get; set; }

        /// <summary>
        /// Security deposit frozen on the baker's account
        /// </summary>
        public long Deposit { get; set; }

        /// <summary>
        /// Reward of the baker for the operation
        /// </summary>
        public long Rewards { get; set; }

        #region injecting
        /// <summary>
        /// Injected historical quote at the time of operation
        /// </summary>
        public QuoteShort Quote { get; set; }
        #endregion
    }
}
