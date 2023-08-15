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
        /// Total number of existing tickets (raw value, not divided by `decimals`). In historical ticket balances this field is omitted.
        /// </summary>
        public string TotalSupply { get; set; }
    }
}
