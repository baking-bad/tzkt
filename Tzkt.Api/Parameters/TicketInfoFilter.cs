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

        /// <summary>
        /// Filter by 32-bit hash of ticket content type.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter typeHash { get; set; }

        /// <summary>
        /// Filter by 32-bit hash of ticket content.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter contentHash { get; set; }

        public bool Empty =>
            id == null &&
            ticketer == null &&
            typeHash == null &&
            contentHash == null;

        public string Normalize(string name)
        {
            return ResponseCacheService.BuildKey("",
                ("id", id), ("ticketer", ticketer), ("typeHash", typeHash), ("contentHash", contentHash));
        }
    }
}
