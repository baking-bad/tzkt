using Netezos.Encoding;

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
        public IMicheline Type { get; set; }

        /// <summary>
        /// Ticket content
        /// </summary>
        public object Content { get; set; }
        
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

        /// <summary>
        /// Total number of existing tickets (raw value, not divided by `decimals`). In historical ticket balances this field is omitted.
        /// </summary>
        public string TotalSupply { get; set; }
    }
}
