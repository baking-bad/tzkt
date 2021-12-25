using System;

namespace Tzkt.Api.Models
{
    public class TokenTransfer
    {
        /// <summary>
        /// Internal TzKT id.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Level of the block, at which the token transfer was made.
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
        /// </summary>
        public string Amount { get; set; }

        /// <summary>
        /// Internal TzKT id of the transaction operation, caused the token transfer.
        /// </summary>
        public int? TransactionId { get; set; }

        /// <summary>
        /// Internal TzKT id of the origination operation, caused the token transfer.
        /// </summary>
        public int? OriginationId { get; set; }

        /// <summary>
        /// Internal TzKT id of the migration operation, caused the token transfer.
        /// </summary>
        public int? MigrationId { get; set; }
    }
}
