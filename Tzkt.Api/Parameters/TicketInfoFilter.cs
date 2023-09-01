using NSwag.Annotations;
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
        /// Filter by ticket content type in Micheline format.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public MichelineParameter rawType { get; set; }

        /// <summary>
        /// Filter by ticket content in Micheline format.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public MichelineParameter rawContent { get; set; }

        /// <summary>
        /// Filter by ticket content in JSON format.  
        /// Note, this parameter supports the following format: `content{.path?}{.mode?}=...`,
        /// so you can specify a path to a particular field to filter by (for example, `?content.color.in=red,green`).  
        /// Click on the parameter to expand more details.
        /// </summary>
        public JsonParameter content { get; set; }

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

        [OpenApiIgnore]
        public bool Empty =>
            id == null &&
            ticketer == null &&
            rawType == null &&
            rawContent == null &&
            content == null &&
            typeHash == null &&
            contentHash == null;

        public string Normalize(string name)
        {
            return ResponseCacheService.BuildKey("",
                ("id", id), ("ticketer", ticketer), ("rawType", rawType), ("rawContent", rawContent),
                ("content", content), ("typeHash", typeHash), ("contentHash", contentHash));
        }
    }
}
