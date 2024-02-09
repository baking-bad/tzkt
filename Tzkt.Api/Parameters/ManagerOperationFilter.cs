using NSwag.Annotations;
using Tzkt.Api.Services;

namespace Tzkt.Api
{
    public class ManagerOperationFilter : INormalizable
    {
        /// <summary>
        /// Filter by internal MvKT id.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int64Parameter id { get; set; }

        /// <summary>
        /// Filter by operation hash.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public OpHashParameter hash { get; set; }

        /// <summary>
        /// Filter by operation counter.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter counter { get; set; }

        /// <summary>
        /// Filter by the domain level.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter level { get; set; }

        /// <summary>
        /// Filter by timestamp (ISO 8601) of the operation.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public TimestampParameter timestamp { get; set; }

        /// <summary>
        /// Filter by operation status (`applied`, `failed`, `backtracked`, `skipped`).  
        /// Click on the parameter to expand more details.
        /// </summary>
        public OperationStatusParameter status { get; set; }

        /// <summary>
        /// Filter by operation sender address.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public AccountParameter sender { get; set; }

        [OpenApiIgnore]
        public virtual bool Empty =>
            id == null &&
            hash == null &&
            counter == null &&
            level == null &&
            timestamp == null &&
            status == null &&
            sender == null;

        public virtual string Normalize(string name)
        {
            return ResponseCacheService.BuildKey("",
                ("id", id), ("hash", hash), ("counter", counter), ("level", level),
                ("timestamp", timestamp), ("status", status), ("sender", sender));
        }
    }
}
