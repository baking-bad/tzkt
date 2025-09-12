using System.Numerics;
using NJsonSchema.Annotations;

namespace Mvkt.Api.Models
{
    public class TokenBalance
    {
        /// <summary>
        /// Internal MvKT id.  
        /// **[sortable]**
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Owner account.  
        /// Click on the field to expand more details.
        /// </summary>
        public Alias Account { get; set; }

        /// <summary>
        /// Token info.  
        /// Click on the field to expand more details.
        /// </summary>
        public TokenInfo Token { get; set; }

        /// <summary>
        /// Balance (raw value, not divided by `decimals`).  
        /// **[sortable]**
        /// </summary>
        [JsonSchemaType(typeof(string), IsNullable = false)]
        public BigInteger Balance { get; set; }

        /// <summary>
        /// Balance value in mumav, based on the current token price.  
        /// **[sortable]**
        /// </summary>
        [JsonSchemaType(typeof(string), IsNullable = true)]
        public BigInteger? BalanceValue { get; set; }

        /// <summary>
        /// Total number of transfers, affecting the token balance.  
        /// **[sortable]**
        /// </summary>
        public int TransfersCount { get; set; }

        /// <summary>
        /// Level of the block where the token balance was first changed.  
        /// **[sortable]**
        /// </summary>
        public int FirstLevel { get; set; }

        /// <summary>
        /// Timestamp of the block where the token balance was first changed.
        /// </summary>
        public DateTime FirstTime { get; set; }

        /// <summary>
        /// Level of the block where the token balance was last changed.  
        /// **[sortable]**
        /// </summary>
        public int LastLevel { get; set; }

        /// <summary>
        /// Timestamp of the block where the token balance was last changed.
        /// </summary>
        public DateTime LastTime { get; set; }
    }
}
