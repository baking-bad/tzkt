using System;

namespace Tzkt.Api.Models
{
    public class TicketTransfer
    {
        /// <summary>
        /// Internal TzKT id.  
        /// **[sortable]**
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Level of the block, at which the ticket transfer was made.  
        /// **[sortable]**
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// Timestamp of the block, at which the ticket transfer was made.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Ticket info.  
        /// Click on the field to expand more details.
        /// </summary>
        public TicketInfo Ticket { get; set; }

        /// <summary>
        /// Sender account.  
        /// Click on the field to expand more details.
        /// </summary>
        public Alias From { get; set; }

        /// <summary>
        /// Target account.  
        /// Click on the field to expand more details.
        /// </summary>
        public Alias To { get; set; }

        /// <summary>
        /// Amount of tickets transferred (raw value, not divided by `decimals`).  
        /// **[sortable]**
        /// </summary>
        public string Amount { get; set; }

        /// <summary>
        /// Internal TzKT id of the transaction operation, caused the ticket transfer.
        /// </summary>
        public long? TransactionId { get; set; }

        /// <summary>
        /// Internal TzKT id of the transfer ticket operation, caused the ticket transfer.
        /// </summary>
        public long? TransferTicketId { get; set; }

        /// <summary>
        /// Internal TzKT id of the smart rollup execute operation, caused the ticket transfer.
        /// </summary>
        public long? SmartRollupExecuteId { get; set; }
    }
}
