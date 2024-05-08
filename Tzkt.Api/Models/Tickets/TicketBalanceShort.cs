using System.Numerics;

namespace Tzkt.Api.Models
{
    public class TicketBalanceShort
    {
        /// <summary>
        /// Internal TzKT id.  
        /// **[sortable]**
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Ticket info.  
        /// Click on the field to expand more details.
        /// </summary>
        public TicketInfoShort Ticket { get; set; }

        /// <summary>
        /// Owner account.  
        /// Click on the field to expand more details.
        /// </summary>
        public Alias Account { get; set; }

        /// <summary>
        /// Balance.  
        /// </summary>
        public BigInteger Balance { get; set; }
    }
}
