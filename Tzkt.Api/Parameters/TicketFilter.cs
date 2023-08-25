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
        /// Filter by ticketer address.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public AccountParameter ticketer { get; set; }

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
        /// Filter by 32-bit hash of ticket content (helpful for searching similar tickets).  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter contentHash { get; set; }

        /// <summary>
        /// Filter by 32-bit hash of ticket content type (helpful for searching same tickets).  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter typeHash { get; set; }

        /// <summary>
        /// Filter by content.  
        /// Note, this parameter supports the following format: `content{.path?}{.mode?}=...`,
        /// so you can specify a path to a particular field to filter by (for example, `?content.in=red,green`).  
        /// Click on the parameter to expand more details.
        /// </summary>
        public JsonParameter content { get; set; }

        public string Normalize(string name)
        {
            return ResponseCacheService.BuildKey("",
                ("id", id), ("ticketer", ticketer), ("firstMinter", firstMinter), ("firstLevel", firstLevel), 
                ("firstTime", firstTime), ("lastLevel", lastLevel), ("lastTime", lastTime), ("contentHash", contentHash),
                ("typeHash", typeHash), ("content", content));
        }
    }
}
