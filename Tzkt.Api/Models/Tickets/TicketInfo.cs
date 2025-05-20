using System.Numerics;
using Netezos.Encoding;
using NJsonSchema.Annotations;

namespace Tzkt.Api.Models
{
    public class TicketInfo
    {
        /// <summary>
        /// Internal TzKT id.
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
        /// Total amount exists.
        /// </summary>
        [JsonSchemaType(typeof(string), IsNullable = false)]
        public BigInteger TotalSupply { get; set; }
    }
}
