using Newtonsoft.Json;
using Tzkt.Api.Services;

namespace Tzkt.Api
{
    public class TicketInfoFilter : INormalizable
    {
        /// <summary>
        /// Filter by internal TzKT id. Note, this is not the same as `ticketId`.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int64Parameter id { get; set; }

        /// <summary>
        /// Filter by contract address.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public AccountParameter contract { get; set; }

        /// <summary>
        /// Filter by ticketId (for FA1.2 tickets ticketId is always `"0"`).  
        /// Click on the parameter to expand more details.
        /// </summary>
        public NatParameter ticketId { get; set; }

        /// <summary>
        /// Filter by metadata. Note, this parameter supports the following format: `ticket.metadata{.path?}{.mode?}=...`,
        /// so you can specify a path to a particular field to filter by, for example: `?ticket.metadata.symbol.in=kUSD,uUSD`.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public JsonParameter metadata { get; set; }

        public string Normalize(string name)
        {
            return ResponseCacheService.BuildKey("",
                ("id", id), ("contract", contract), ("ticketId", ticketId), ("metadata", metadata));
        }
    }
}
