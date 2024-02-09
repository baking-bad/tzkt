namespace Mvkt.Api.Models
{
    public class SrMessage
    {
        /// <summary>
        /// Internal MvKT id.  
        /// **[sortable]**
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Level of the block where the message was pushed.  
        /// **[sortable]**
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// Timestamp of the block where the message was pushed.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Type of the message (`level_start`, `level_info`, `level_end`, `transfer`, `external`, `migration`).
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// For `level_info` messages only. Hash of the predecessor block.
        /// </summary>
        public string PredecessorHash { get; set; }

        /// <summary>
        /// For `level_info` messages only. Timestamp of the predecessor block.
        /// </summary>
        public DateTime? PredecessorTimestamp { get; set; }

        /// <summary>
        /// For `transfer` messages only. Account, initiated the operation.
        /// </summary>
        public Alias Initiator { get; set; }

        /// <summary>
        /// For `transfer` messages only. Smart contract, sent the internal transaction.
        /// </summary>
        public Alias Sender { get; set; }

        /// <summary>
        /// For `transfer` messages only. Smart rollup to which the internal transaction was sent.
        /// </summary>
        public Alias Target { get; set; }

        /// <summary>
        /// For `transfer` messages only. Entrypoint called in the target rollup
        /// </summary>
        public string Entrypoint { get; set; }

        /// <summary>
        /// For `transfer` messages only. Value passed to the called entrypoint. Note: you can configure parameters format by setting `micheline` query parameter.
        /// </summary>
        public object Parameter { get; set; }

        /// <summary>
        /// For `external` messages only. Payload bytes (in base64).
        /// </summary>
        public byte[] Payload { get; set; }

        /// <summary>
        /// For `migration` messages only. Version of the new protocol (e.g. 'nairobi_017').
        /// </summary>
        public string Protocol { get; set; }
    }
}
