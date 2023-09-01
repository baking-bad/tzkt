using Netezos.Encoding;

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
        /// Contract, issued the ticket.
        /// </summary>
        public Alias Ticketer { get; set; }
        
        /// <summary>
        /// Ticket content type in Micheline format.
        /// </summary>
        public IMicheline RawType { get; set; }

        /// <summary>
        /// Ticket content in Micheline format.
        /// </summary>
        public IMicheline RawContent { get; set; }

        /// <summary>
        /// Ticket content in JSON format.
        /// </summary>
        public RawJson Content { get; set; }

        /// <summary>
        /// 32-bit hash of the ticket content type.
        /// </summary>
        public int TypeHash { get; set; }

        /// <summary>
        /// 32-bit hash of the ticket content.
        /// </summary>
        public int ContentHash { get; set; }

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
        /// Total amount minted.
        /// </summary>
        public string TotalMinted { get; set; }

        /// <summary>
        /// Total amount burned.
        /// </summary>
        public string TotalBurned { get; set; }

        /// <summary>
        /// Total amount exists.
        /// </summary>
        public string TotalSupply { get; set; }
    }
}
