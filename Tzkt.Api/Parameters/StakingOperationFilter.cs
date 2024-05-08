using Tzkt.Api.Services;

namespace Tzkt.Api
{
    public class StakingOperationFilter : ManagerOperationFilter
    {
        /// <summary>
        /// Filter by any of the specified fields (`sender`, or `baker`).
        /// Example: `anyof.sender.baker=tz1...` will return operations where `sender` OR `baker` is equal to the specified value.
        /// This parameter is useful when you need to get all operations somehow related to the account in a single request.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public AnyOfParameter anyof { get; set; }

        /// <summary>
        /// Filter by baker address.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public AccountParameter baker { get; set; }

        /// <summary>
        /// Filter by staking action (`stake`, `unstake`, `finalize`).  
        /// Click on the parameter to expand more details.
        /// </summary>
        public StakingActionParameter action { get; set; }

        public override bool Empty =>
            base.Empty &&
            anyof == null &&
            baker == null &&
            action == null;

        public override string Normalize(string name)
        {
            return ResponseCacheService.BuildKey("",
                ("id", id), ("hash", hash), ("counter", counter), ("level", level),
                ("timestamp", timestamp), ("status", status), ("sender", sender),
                ("anyof", anyof), ("baker", baker), ("action", action));
        }
    }
}
