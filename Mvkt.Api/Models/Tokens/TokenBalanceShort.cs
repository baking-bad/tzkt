using System.Numerics;
using NJsonSchema.Annotations;

namespace Mvkt.Api.Models
{
    public class TokenBalanceShort
    {
        /// <summary>
        /// Owner account.  
        /// Click on the field to expand more details.
        /// </summary>
        public Alias Account { get; set; }

        /// <summary>
        /// Token info.  
        /// Click on the field to expand more details.
        /// </summary>
        public TokenInfoShort Token { get; set; }

        /// <summary>
        /// Balance (raw value, not divided by `decimals`).  
        /// **[sortable]**
        /// </summary>
        [JsonSchemaType(typeof(string), IsNullable = false)]
        public BigInteger Balance { get; set; }
    }
}
