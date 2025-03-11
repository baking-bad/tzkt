using System.Numerics;
using NJsonSchema.Annotations;

namespace Tzkt.Api.Models
{
    public class TokenBalanceShort
    {
        /// <summary>
        /// Owner account.  
        /// Click on the field to expand more details.
        /// </summary>
        public required Alias Account { get; set; }

        /// <summary>
        /// Token info.  
        /// Click on the field to expand more details.
        /// </summary>
        public required TokenInfoShort Token { get; set; }

        /// <summary>
        /// Balance (raw value, not divided by `decimals`).  
        /// **[sortable]**
        /// </summary>
        [JsonSchemaType(typeof(string), IsNullable = false)]
        public BigInteger Balance { get; set; }
    }
}
