using NSwag.Annotations;
using Tzkt.Api.Services;

namespace Tzkt.Api
{
    public class SrStakerFilter : INormalizable
    {
        /// <summary>
        /// Filter by internal TzKT id.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter id { get; set; }

        /// <summary>
        /// Filter by smart rollup staker address.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public AddressParameter address { get; set; }

        /// <summary>
        /// Filter by staker's bond status (`active`, `returned`, or `lost`).  
        /// Click on the parameter to expand more details.
        /// </summary>
        public SrBondStatusParameter bondStatus { get; set; }

        [OpenApiIgnore]
        public bool Empty =>
            id == null &&
            address == null &&
            bondStatus == null;

        public string Normalize(string name)
        {
            return ResponseCacheService.BuildKey("",
                ("id", id), ("address", address), ("bondStatus", bondStatus));
        }
    }
}
