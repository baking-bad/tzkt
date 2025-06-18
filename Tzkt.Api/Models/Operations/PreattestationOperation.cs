namespace Tzkt.Api.Models
{
    public class PreattestationOperation : Operation
    {
        /// <summary>
        /// Type of the operation, `preattestation`
        /// </summary>
        public override string Type => ActivityTypes.Preattestation;

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
        /// Information about the baker who sent the operation
        /// </summary>
        public required Alias Delegate { get; set; }

        /// <summary>
        /// Number of assigned attestation slots to the baker who sent the operation
        /// </summary>
        public int Slots { get; set; }

        #region injecting
        /// <summary>
        /// Injected historical quote at the time of operation
        /// </summary>
        public QuoteShort? Quote { get; set; }
        #endregion
    }
}
