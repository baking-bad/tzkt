using NSwag.Annotations;
using Tzkt.Api.Services;

namespace Tzkt.Api
{
    public class SrCommitmentInfoFilter : INormalizable
    {
        /// <summary>
        /// Filter by internal TzKT id.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter? id { get; set; }

        /// <summary>
        /// Filter by commitment hash.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Src1HashParameter? hash { get; set; }

        [OpenApiIgnore]
        public bool Empty => id == null && hash == null;

        public string Normalize(string name)
        {
            return ResponseCacheService.BuildKey("", ("id", id), ("hash", hash));
        }
    }
}
