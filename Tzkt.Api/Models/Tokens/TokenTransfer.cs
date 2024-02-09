using System.Numerics;
using NJsonSchema.Annotations;

namespace Tzkt.Api.Models
{
    public class TokenTransfer
    {
        /// <summary>
        /// Internal MvKT id.  
        /// **[sortable]**
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Level of the block, at which the token transfer was made.  
        /// **[sortable]**
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// Timestamp of the block, at which the token transfer was made.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Token info.  
        /// Click on the field to expand more details.
        /// </summary>
        public TokenInfo Token { get; set; }

        /// <summary>
        /// Sender account.  
        /// Click on the field to expand more details.
        /// </summary>
        public Alias From { get; set; }

        /// <summary>
        /// Target account.  
        /// Click on the field to expand more details.
        /// </summary>
        public Alias To { get; set; }

        /// <summary>
        /// Amount of tokens transferred (raw value, not divided by `decimals`).  
        /// **[sortable]**
        /// </summary>
        [JsonSchemaType(typeof(string), IsNullable = false)]
        public BigInteger Amount { get; set; }

        /// <summary>
        /// Internal MvKT id of the transaction operation, caused the token transfer.
        /// </summary>
        public long? TransactionId { get; set; }

        /// <summary>
        /// Internal MvKT id of the origination operation, caused the token transfer.
        /// </summary>
        public long? OriginationId { get; set; }

        /// <summary>
        /// Internal MvKT id of the migration operation, caused the token transfer.
        /// </summary>
        public long? MigrationId { get; set; }
    }
}
