using Newtonsoft.Json;
using Tzkt.Api.Services;

namespace Tzkt.Api
{
    public class TokenInfoFilter : INormalizable
    {
        /// <summary>
        /// Filter by internal TzKT id. Note, this is not the same as `tokenId`.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int64Parameter id { get; set; }

        /// <summary>
        /// Filter by contract address.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public AccountParameter contract { get; set; }

        /// <summary>
        /// Filter by tokenId (for FA1.2 tokens tokenId is always `"0"`).  
        /// Click on the parameter to expand more details.
        /// </summary>
        public NatParameter tokenId { get; set; }

        /// <summary>
        /// Filter by token standard (`fa1.2` or `fa2`).  
        /// Click on the parameter to expand more details.
        /// </summary>
        public TokenStandardParameter standard { get; set; }

        /// <summary>
        /// Filter by metadata. Note, this parameter supports the following format: `token.metadata{.path?}{.mode?}=...`,
        /// so you can specify a path to a particular field to filter by, for example: `?token.metadata.symbol.in=kUSD,uUSD`.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public JsonParameter metadata { get; set; }

        [JsonIgnore]
        public bool HasFilters => contract != null || tokenId != null || standard != null;

        public string Normalize(string name)
        {
            return ResponseCacheService.BuildKey("",
                ("id", id), ("contract", contract), ("tokenId", tokenId), ("standard", standard), ("metadata", metadata));
        }
    }
}
