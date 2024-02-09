using Mvkt.Api.Services;

namespace Mvkt.Api
{
    public class SrOperationFilter : ManagerOperationFilter
    {
        /// <summary>
        /// Filter by rollup address.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public SmartRollupParameter rollup { get; set; }

        public override bool Empty => base.Empty && rollup == null;

        public override string Normalize(string name)
        {
            return ResponseCacheService.BuildKey("",
                ("id", id), ("hash", hash), ("counter", counter), ("level", level),
                ("timestamp", timestamp), ("status", status), ("sender", sender),
                ("rollup", rollup));
        }
    }
}
