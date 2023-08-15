namespace Tzkt.Api.Models
{
    public class TicketBalanceShort
    {
        /// <summary>
        /// Owner account.  
        /// Click on the field to expand more details.
        /// </summary>
        public Alias Account { get; set; }

        /// <summary>
        /// Ticket info.  
        /// Click on the field to expand more details.
        /// </summary>
        public TicketInfo Ticket { get; set; }

        /// <summary>
        /// Balance (raw value, not divided by `decimals`).  
        /// **[sortable]**
        /// </summary>
        public string Balance { get; set; }
    }
}
