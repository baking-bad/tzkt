using System;
using NJsonSchema.Annotations;

namespace Tzkt.Api.Models
{
    public class Ticket
    {
        /// <summary>
        /// Internal TzKT id.  
        /// **[sortable]**
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Contract, created the ticket.
        /// </summary>
        public Alias Ticketer { get; set; }
        
        
        /// <summary>
        /// Micheline type of the content
        /// </summary>
        public RawJson ContentType { get; set; }

        /// <summary>
        /// Ticket content
        /// </summary>
        public object Content { get; set; }

        /// <summary>
        /// Account, minted the ticket first.
        /// </summary>
        public Alias FirstMinter { get; set; }

        /// <summary>
        /// Level of the block where the ticket was first seen.  
        /// **[sortable]**
        /// </summary>
        public int FirstLevel { get; set; }

        /// <summary>
        /// Timestamp of the block where the ticket was first seen.
        /// </summary>
        public DateTime FirstTime { get; set; }

        /// <summary>
        /// Level of the block where the ticket was last seen.  
        /// **[sortable]**
        /// </summary>
        public int LastLevel { get; set; }

        /// <summary>
        /// Timestamp of the block where the ticket was last seen.
        /// </summary>
        public DateTime LastTime { get; set; }

        /// <summary>
        /// Total number of transfers.  
        /// **[sortable]**
        /// </summary>
        public int TransfersCount { get; set; }

        /// <summary>
        /// Total number of holders ever seen.  
        /// **[sortable]**
        /// </summary>
        public int BalancesCount { get; set; }

        /// <summary>
        /// Total number of current holders.  
        /// **[sortable]**
        /// </summary>
        public int HoldersCount { get; set; }

        /// <summary>
        /// Total number of minted tickets (raw value, not divided by `decimals`).
        /// </summary>
        public string TotalMinted { get; set; }

        /// <summary>
        /// Total number of burned tickets (raw value, not divided by `decimals`).
        /// </summary>
        public string TotalBurned { get; set; }

        /// <summary>
        /// Total number of existing tickets (raw value, not divided by `decimals`).
        /// </summary>
        public string TotalSupply { get; set; }
    }
}
