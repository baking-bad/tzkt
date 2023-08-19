using Newtonsoft.Json;
using Tzkt.Api.Services;

namespace Tzkt.Api
{
    public class TicketInfoFilter : INormalizable
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
        
        //TODO Add that everywhere
        /// <summary>
        /// Filter by 32-bit hash of ticket content (helpful for searching similar tickets).  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter contentHash { get; set; }

        /// <summary>
        /// Filter by 32-bit hash of ticket content type (helpful for searching same tickets).  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter contentTypeHash { get; set; }

        public string Normalize(string name)
        {
            return ResponseCacheService.BuildKey("",
                ("id", id), ("ticketer", ticketer), ("contentHash", contentHash), ("contentTypeHash", contentTypeHash));
        }
    }
}
