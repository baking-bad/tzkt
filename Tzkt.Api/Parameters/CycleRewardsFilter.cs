using NSwag.Annotations;
using Tzkt.Api.Services;

namespace Tzkt.Api
{
    public class CycleRewardsFilter : INormalizable
    {
        /// <summary>
        /// Filter by cycle.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter? cycle { get; set; }

        /// <summary>
        /// Filter by baker.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public AccountParameter? baker { get; set; }

        [OpenApiIgnore]
        public virtual bool Empty =>
            cycle == null &&
            baker == null;

        public virtual string Normalize(string name)
        {
            return ResponseCacheService.BuildKey("",
                ("cycle", cycle), ("baker", baker));
        }
    }
}
