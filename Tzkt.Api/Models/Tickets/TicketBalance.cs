using System.Numerics;

namespace Tzkt.Api.Models
{
    public class TicketBalance
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
        public TicketInfo Ticket { get; set; }

        /// <summary>
        /// Owner account.  
        /// Click on the field to expand more details.
        /// </summary>
        public Alias Account { get; set; }

        /// <summary>
        /// Balance.  
        /// **[sortable]**
        /// </summary>
        public BigInteger Balance { get; set; }

        /// <summary>
        /// Total number of transfers, affecting the ticket balance.  
        /// **[sortable]**
        /// </summary>
        public int TransfersCount { get; set; }

        /// <summary>
        /// Level of the block where the ticket balance was first changed.  
        /// **[sortable]**
        /// </summary>
        public int FirstLevel { get; set; }

        /// <summary>
        /// Timestamp of the block where the ticket balance was first changed.
        /// </summary>
        public DateTime FirstTime { get; set; }

        /// <summary>
        /// Level of the block where the ticket balance was last changed.  
        /// **[sortable]**
        /// </summary>
        public int LastLevel { get; set; }

        /// <summary>
        /// Timestamp of the block where the ticket balance was last changed.
        /// </summary>
        public DateTime LastTime { get; set; }
    }
}
