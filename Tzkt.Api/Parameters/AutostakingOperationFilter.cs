using NSwag.Annotations;
using Tzkt.Api.Services;

namespace Tzkt.Api
{
    public class AutostakingOperationFilter : INormalizable
    {
        /// <summary>
        /// Filter by internal TzKT id.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int64Parameter? id { get; set; }

        /// <summary>
        /// Filter by level of the block where the operation happened.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter? level { get; set; }

        /// <summary>
        /// Filter by timestamp (ISO 8601) of the block where the operation happened.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public TimestampParameter? timestamp { get; set; }

        /// <summary>
        /// Filter by baker.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public AccountParameter? baker { get; set; }

        /// <summary>
        /// Filter by autostaking action.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public StakingActionParameter? action { get; set; }

        /// <summary>
        /// Filter by amount.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int64Parameter? amount { get; set; }

        /// <summary>
        /// Filter by number of staking updates.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter? stakingUpdatesCount { get; set; }

        [OpenApiIgnore]
        public bool Empty =>
            id == null &&
            level == null &&
            timestamp == null &&
            baker == null &&
            action == null &&
            amount == null &&
            stakingUpdatesCount == null;

        public string Normalize(string name)
        {
            return ResponseCacheService.BuildKey("",
                ("id", id), ("level", level), ("timestamp", timestamp), ("baker", baker),
                ("action", action), ("amount", amount), ("stakingUpdatesCount", stakingUpdatesCount));
        }
    }
}
