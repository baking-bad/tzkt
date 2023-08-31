using Tzkt.Api.Services;

namespace Tzkt.Api
{
    public class TicketFilter : INormalizable
    {
        /// <summary>
        /// Filter by internal TzKT id.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int64Parameter id { get; set; }

        /// <summary>
        /// Filter by ticketer address.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public AccountParameter ticketer { get; set; }

        /// <summary>
        /// Filter by content.  
        /// Note, this parameter supports the following format: `content{.path?}{.mode?}=...`,
        /// so you can specify a path to a particular field to filter by (for example, `?content.in=red,green`).  
        /// Click on the parameter to expand more details.
        /// </summary>
        public JsonParameter content { get; set; }

        /// <summary>
        /// Filter by 32-bit hash of ticket content type (helpful for searching same tickets).  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter typeHash { get; set; }

        /// <summary>
        /// Filter by 32-bit hash of ticket content (helpful for searching similar tickets).  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter contentHash { get; set; }

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

        public bool Empty =>
            id == null &&
            ticketer == null &&
            content == null &&
            typeHash == null &&
            contentHash == null &&
            firstMinter == null &&
            firstLevel == null &&
            firstTime == null &&
            lastLevel == null &&
            lastTime == null;

        public string Normalize(string name)
        {
            return ResponseCacheService.BuildKey("",
                ("id", id), ("ticketer", ticketer), ("content", content), ("typeHash", typeHash), ("contentHash", contentHash),
                ("firstMinter", firstMinter), ("firstLevel", firstLevel), ("firstTime", firstTime), ("lastLevel", lastLevel),
                ("lastTime", lastTime));
        }
    }
}
