using NSwag.Annotations;
using Tzkt.Api.Services;

namespace Tzkt.Api
{
    public class SrMessageFilter : INormalizable
    {
        /// <summary>
        /// Filter by internal TzKT id.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter id { get; set; }

        /// <summary>
        /// Filter by level of the block, where the message was pushed.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter level { get; set; }

        /// <summary>
        /// Filter by timestamp of the block, where the message was pushed.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public TimestampParameter timestamp { get; set; }

        /// <summary>
        /// Filter by inbox message type (`level_start`, `level_info`, `level_end`, `transfer`, `external`, `migration`).  
        /// Click on the parameter to expand more details.
        /// </summary>
        public SrMessageTypeParameter type { get; set; }

        [OpenApiIgnore]
        public bool Empty =>
            id == null &&
            level == null &&
            timestamp == null &&
            type == null;

        public string Normalize(string name)
        {
            return ResponseCacheService.BuildKey("",
                ("id", id), ("level", level), ("timestamp", timestamp), ("type", type));
        }
    }
}
