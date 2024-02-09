using Netmavryk.Encoding;
using NJsonSchema.Annotations;

namespace Mvkt.Api.Models
{
    public class TicketInfoShort
    {
        /// <summary>
        /// Internal MvKT id.
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
        [JsonSchemaType(typeof(object), IsNullable = true)]
        public RawJson Content { get; set; }
        
        /// <summary>
        /// 32-bit hash of the ticket content type.
        /// This field can be used for searching similar tickets (which have the same type).
        /// </summary>
        public int TypeHash { get; set; }

        /// <summary>
        /// 32-bit hash of the ticket content.
        /// This field can be used for searching same tickets (which have the same content).
        /// </summary>
        public int ContentHash { get; set; }
    }
}
