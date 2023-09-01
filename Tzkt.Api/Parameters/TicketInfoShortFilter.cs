using NSwag.Annotations;
using Tzkt.Api.Services;

namespace Tzkt.Api
{
    public class TicketInfoShortFilter : INormalizable
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

        [OpenApiIgnore]
        public bool Empty =>
            id == null &&
            ticketer == null;

        public string Normalize(string name)
        {
            return ResponseCacheService.BuildKey("",
                ("id", id), ("ticketer", ticketer));
        }
    }
}
