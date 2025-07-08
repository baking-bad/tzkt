using NSwag.Annotations;
using Tzkt.Api.Services;

namespace Tzkt.Api
{
    public class DelegatorRewardsFilter : CycleRewardsFilter
    {
        /// <summary>
        /// Filter by delegator.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public AccountParameter? delegator { get; set; }

        [OpenApiIgnore]
        public override bool Empty =>
            base.Empty && delegator == null;

        public override string Normalize(string name) =>
            ResponseCacheService.BuildKey(base.Normalize(name), ("delegator", delegator));
    }
}
