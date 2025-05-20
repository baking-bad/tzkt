using System.Numerics;
using Netezos.Encoding;
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
        /// Contract, issued the ticket.
        /// </summary>
        public required Alias Ticketer { get; set; }

        /// <summary>
        /// Ticket content type in Micheline format.
        /// </summary>
        public required IMicheline RawType { get; set; }

        /// <summary>
        /// Ticket content in Micheline format.
        /// </summary>
        public required IMicheline RawContent { get; set; }

        /// <summary>
        /// Ticket content in JSON format.
        /// </summary>
        public RawJson? Content { get; set; }

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
        public required Alias FirstMinter { get; set; }

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
        [JsonSchemaType(typeof(string), IsNullable = false)]
        public BigInteger TotalMinted { get; set; }

        /// <summary>
        /// Total amount burned.
        /// </summary>
        [JsonSchemaType(typeof(string), IsNullable = false)]
        public BigInteger TotalBurned { get; set; }

        /// <summary>
        /// Total amount exists.
        /// </summary>
        [JsonSchemaType(typeof(string), IsNullable = false)]
        public BigInteger TotalSupply { get; set; }
    }
}
