using NSwag.Annotations;
using Mvkt.Api.Services;

namespace Mvkt.Api
{
    public class UnstakeRequestFilter : INormalizable
    {
        /// <summary>
        /// Filter by internal TzKT id.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter id { get; set; }

        /// <summary>
        /// Filter by cycle.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter cycle { get; set; }

        /// <summary>
        /// Filter by related baker.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public AccountParameter baker { get; set; }

        /// <summary>
        /// Filter by related staker.
        /// If staker is null, then it's aggregated unstaked deposits for the baker.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public AccountParameter staker { get; set; }

        /// <summary>
        /// Filter by requested amount.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int64Parameter requestedAmount { get; set; }

        /// <summary>
        /// Filter by restaked amount.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int64Parameter restakedAmount { get; set; }

        /// <summary>
        /// Filter by finalized amount.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int64Parameter finalizedAmount { get; set; }

        /// <summary>
        /// Filter by slashed amount.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int64Parameter slashedAmount { get; set; }

        /// <summary>
        /// Filter by protocol rounding error.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int64NullParameter roundingError { get; set; }

        /// <summary>
        /// Filter by actual amount.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int64Parameter actualAmount { get; set; }

        /// <summary>
        /// Filter by status.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public UnstakeRequestStatusParameter status { get; set; }

        /// <summary>
        /// Filter by staking updates count.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter updatesCount { get; set; }

        /// <summary>
        /// Filter by level of the block where the unstake request was created.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter firstLevel { get; set; }

        /// <summary>
        /// Filter by timestamp (ISO 8601) of the block where the unstake request was created.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public TimestampParameter firstTime { get; set; }

        /// <summary>
        /// Filter by level of the block where the unstake request was last updated.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter lastLevel { get; set; }

        /// <summary>
        /// Filter by timestamp (ISO 8601) of the block where the unstake request was last updated.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public TimestampParameter lastTime { get; set; }

        [OpenApiIgnore]
        public bool Empty =>
            id == null &&
            cycle == null &&
            baker == null &&
            staker == null &&
            requestedAmount == null &&
            restakedAmount == null &&
            finalizedAmount == null &&
            slashedAmount == null &&
            roundingError == null &&
            updatesCount == null &&
            actualAmount == null &&
            status == null &&
            firstLevel == null &&
            firstTime == null &&
            lastLevel == null &&
            lastTime == null;

        public string Normalize(string name)
        {
            return ResponseCacheService.BuildKey("",
                ("id", id), ("cycle", cycle), ("baker", baker), ("staker", staker), ("requestedAmount", requestedAmount),
                ("restakedAmount", restakedAmount), ("finalizedAmount", finalizedAmount), ("slashedAmount", slashedAmount),
                ("roundingError", roundingError), ("actualAmount", actualAmount), ("status", status), ("updatesCount", updatesCount),
                ("firstLevel", firstLevel), ("firstTime", firstTime), ("lastLevel", lastLevel), ("lastTime", lastTime));
        }
    }
}
