using System;

namespace Tzkt.Api.Models
{
    public class DrainDelegateOperation : Operation
    {
        /// <summary>
        /// Type of the operation, `drain_delegate`
        /// </summary>
        public override string Type => OpTypes.DrainDelegate;

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
        /// Information about the drained delegate
        /// </summary>
        public Alias Delegate { get; set; }

        /// <summary>
        /// Information about the recipient account
        /// </summary>
        public Alias Target { get; set; }

        /// <summary>
        /// Amount sent from the drained baker to the target
        /// </summary>
        public long Amount { get; set; }

        /// <summary>
        /// Amount sent from the drained baker to the block baker
        /// </summary>
        public long Fee { get; set; }

        #region injecting
        /// <summary>
        /// Injected historical quote at the time of operation
        /// </summary>
        public QuoteShort Quote { get; set; }
        #endregion
    }
}
