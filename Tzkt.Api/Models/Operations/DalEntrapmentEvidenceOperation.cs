namespace Tzkt.Api.Models
{
    public class DalEntrapmentEvidenceOperation : Operation
    {
        /// <summary>
        /// Type of the operation, `dal_entrapment_evidence`
        /// </summary>
        public override string Type => OpTypes.DalEntrapmentEvidence;

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
        /// Information about the baker, produced the block, in which the evidence was included
        /// </summary>
        public Alias Accuser { get; set; }

        /// <summary>
        /// Information about the baker, accused for attesting trapped shard
        /// </summary>
        public Alias Offender { get; set; }

        /// <summary>
        /// Height of the block from the genesis, where the trap was attested
        /// </summary>
        public int TrapLevel { get; set; }

        /// <summary>
        /// Trap slot index
        /// </summary>
        public int TrapSlotIndex { get; set; }

        #region injecting
        /// <summary>
        /// Injected historical quote at the time of operation
        /// </summary>
        public QuoteShort Quote { get; set; }
        #endregion
    }
}
