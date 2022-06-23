using System.ComponentModel.DataAnnotations;
using Tzkt.Api.Services;

namespace Tzkt.Api
{
    public class Pagination : INormalizable
    {
        /// <summary>
        /// Sorts items (asc or desc) by the specified field.
        /// You can see what fields can be used for sorting in the response description, below.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public SortParameter sort { get; set; }

        /// <summary>
        /// Specifies which or how many items should be skipped.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public OffsetParameter offset { get; set; }

        /// <summary>
        /// Maximum number of items to return.
        /// </summary>
        [Range(0, 10000)]
        public int limit { get; set; } = 100;

        public string Normalize(string name)
        {
            return ResponseCacheService.BuildKey("", ("sort", sort), ("offset", offset), ("limit", limit));
        }
    }
}
