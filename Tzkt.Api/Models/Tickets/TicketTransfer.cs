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
        /// Level of the block, at which the transfer was made.  
        /// **[sortable]**
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// Timestamp of the block, at which the transfer was made.
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
        /// Recepient account.  
        /// Click on the field to expand more details.
        /// </summary>
        public Alias To { get; set; }

        /// <summary>
        /// Amount of tickets transferred.  
        /// **[sortable]**
        /// </summary>
        public string Amount { get; set; }

        /// <summary>
        /// Internal TzKT id of the transaction operation, caused the ticket transfer.
        /// </summary>
        public long? TransactionId { get; set; }

        /// <summary>
        /// Internal TzKT id of the transfer_ticket operation, caused the ticket transfer.
        /// </summary>
        public long? TransferTicketId { get; set; }

        /// <summary>
        /// Internal TzKT id of the smart_rollup_execute operation, caused the ticket transfer.
        /// </summary>
        public long? SmartRollupExecuteId { get; set; }
    }
}
