using NSwag.Annotations;
using Mvkt.Api.Services;

namespace Mvkt.Api
{
    public class SrFilter : INormalizable
    {
        /// <summary>
        /// Filter by internal MvKT id.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter id { get; set; }

        /// <summary>
        /// Filter by smart rollup address.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public AddressParameter address { get; set; }

        /// <summary>
        /// Filter by smart rollup creator.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public AccountParameter creator { get; set; }

        /// <summary>
        /// Filter by level of the block, where the rollup was first seen.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter firstActivity { get; set; }

        /// <summary>
        /// Filter by timestamp of the block, where the rollup was first seen.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public TimestampParameter firstActivityTime { get; set; }

        /// <summary>
        /// Filter by level of the block, where the rollup was last seen.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter lastActivity { get; set; }

        /// <summary>
        /// Filter by timestamp of the block, where the rollup was last seen.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public TimestampParameter lastActivityTime { get; set; }

        [OpenApiIgnore]
        public bool Empty =>
            id == null &&
            address == null &&
            creator == null &&
            firstActivity == null &&
            firstActivityTime == null &&
            lastActivity == null &&
            lastActivityTime == null;

        public string Normalize(string name)
        {
            return ResponseCacheService.BuildKey("",
                ("id", id), ("address", address), ("creator", creator), ("firstActivity", firstActivity),
                ("firstActivityTime", firstActivityTime), ("lastActivity", lastActivity), ("lastActivityTime", lastActivityTime));
        }
    }
}
