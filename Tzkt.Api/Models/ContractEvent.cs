using System;

namespace Tzkt.Api.Models
{
    public class ContractEvent
    {
        /// <summary>
        /// Internal TzKT id.  
        /// **[sortable]**
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Level of the block, at which the event was emitted.  
        /// **[sortable]**
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// Timestamp of the block, at which the event was emitted.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Contract emitted the event.  
        /// Click on the field to expand more details.
        /// </summary>
        public Alias Contract { get; set; }

        /// <summary>
        /// Hash of the contract code.
        /// </summary>
        public int CodeHash { get; set; }

        /// <summary>
        /// Event tag.
        /// </summary>
        public string Tag { get; set; }

        /// <summary>
        /// Event payload in human-readable JSON format.  
        /// **[sortable]**
        /// </summary>
        public object Payload { get; set; }

        /// <summary>
        /// Internal TzKT id of the transaction operation, caused the event.
        /// </summary>
        public long TransactionId { get; set; }

        /// <summary>
        /// Michelson type of the payload.  
        /// **Must be explicitly selected**
        /// </summary>
        public object Type { get; set; }

        /// <summary>
        /// Payload in raw Micheline format.  
        /// **Must be explicitly selected**
        /// </summary>
        public object RawPayload { get; set; }
    }
}
