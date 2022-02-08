using NJsonSchema.Annotations;

namespace Tzkt.Api.Models
{
    public class TokenInfo
    {
        /// <summary>
        /// Internal TzKT id (not the same as `tokenId`).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Contract, created the token.
        /// </summary>
        public Alias Contract { get; set; }

        /// <summary>
        /// Token id, unique within the contract.
        /// </summary>
        public string TokenId { get; set; }

        /// <summary>
        /// Token standard (either `fa1.2` or `fa2`).
        /// </summary>
        public string Standard { get; set; }

        /// <summary>
        /// Token metadata.  
        /// **[sortable]**
        /// </summary>
        [JsonSchemaType(typeof(object), IsNullable = true)]
        public RawJson Metadata { get; set; }
    }
}
