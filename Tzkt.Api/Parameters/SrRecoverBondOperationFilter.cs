using Tzkt.Api.Services;

namespace Tzkt.Api
{
    public class SrRecoverBondOperationFilter : SrOperationFilter
    {
        /// <summary>
        /// Filter by any of the specified fields (`sender` or `staker`).
        /// Example: `anyof.sender.staker=mv1...` will return operations where `sender` OR `staker` is equal to the specified value.
        /// This parameter is useful when you need to get all operations somehow related to the account in a single request.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public AnyOfParameter anyof { get; set; }

        /// <summary>
        /// Filter by staker address.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public AccountParameter staker { get; set; }

        public override bool Empty => base.Empty && staker == null && anyof == null;

        public override string Normalize(string name)
        {
            return ResponseCacheService.BuildKey("",
                ("id", id), ("hash", hash), ("counter", counter), ("level", level),
                ("timestamp", timestamp), ("status", status), ("sender", sender),
                ("rollup", rollup), ("anyof", anyof), ("staker", staker));
        }
    }
}
