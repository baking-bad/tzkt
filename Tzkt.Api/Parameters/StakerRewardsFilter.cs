using NSwag.Annotations;
using Tzkt.Api.Services;

namespace Tzkt.Api
{
    public class StakerRewardsFilter : INormalizable
    {
        /// <summary>
        /// Filter by internal TzKT id.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter? id { get; set; }

        /// <summary>
        /// Filter by cycle.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter? cycle { get; set; }

        /// <summary>
        /// Filter by staker.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public AccountParameter? staker { get; set; }

        /// <summary>
        /// Filter by baker.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public AccountParameter? baker { get; set; }

        [OpenApiIgnore]
        public bool Empty =>
            id == null &&
            cycle == null &&
            staker == null &&
            baker == null;

        public string Normalize(string name)
        {
            return ResponseCacheService.BuildKey("",
                ("id", id), ("cycle", cycle), ("staker", staker), ("baker", baker));
        }
    }
}
