using System.Text.Json.Serialization;
using Tzkt.Api.Services;

namespace Tzkt.Api
{
    public class TicketFilter : INormalizable
    {
        /// <summary>
        /// Filter by internal TzKT id. Note, this is not the same as `ticketId` nat value.  
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
        /// Filter by address of the first minter.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public AccountParameter firstMinter { get; set; }

        /// <summary>
        /// Filter by level of the block where the ticket was first seen.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter firstLevel { get; set; }

        /// <summary>
        /// Filter by timestamp (ISO 8601) of the block where the ticket was first seen.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public TimestampParameter firstTime { get; set; }

        /// <summary>
        /// Filter by level of the block where the ticket was last seen.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter lastLevel { get; set; }

        /// <summary>
        /// Filter by timestamp (ISO 8601) of the block where the ticket was last seen.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public TimestampParameter lastTime { get; set; }

        /// <summary>
        /// Filter by metadata.  
        /// Note, this parameter supports the following format: `metadata{.path?}{.mode?}=...`,
        /// so you can specify a path to a particular field to filter by (for example, `?metadata.symbol.in=kUSD,uUSD`).  
        /// Click on the parameter to expand more details.
        /// </summary>
        public JsonParameter metadata { get; set; }

        [JsonIgnore]
        public Int32NullParameter indexedAt { get; set; }

        public string Normalize(string name)
        {
            return ResponseCacheService.BuildKey("",
                ("id", id), ("contract", contract), ("ticketId", ticketId), ("firstMinter", firstMinter), ("firstLevel", firstLevel), 
                ("firstTime", firstTime), ("lastLevel", lastLevel), ("lastTime", lastTime), ("metadata", metadata), ("indexedAt", indexedAt));
        }
    }
}
