﻿using System.Numerics;
using NJsonSchema.Annotations;

namespace Tzkt.Api.Models
{
    public class TokenTransferActivity : Activity
    {
        /// <summary>
        /// Activity type
        /// </summary>
        public override string Type => ActivityTypes.TokenTransfer;

        /// <summary>
        /// Internal TzKT id.  
        /// **[sortable]**
        /// </summary>
        public override long Id { get; set; }

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
        public required TokenInfo Token { get; set; }

        /// <summary>
        /// Sender account.  
        /// Click on the field to expand more details.
        /// </summary>
        public Alias? From { get; set; }

        /// <summary>
        /// Target account.  
        /// Click on the field to expand more details.
        /// </summary>
        public Alias? To { get; set; }

        /// <summary>
        /// Amount of tokens transferred (raw value, not divided by `decimals`).  
        /// **[sortable]**
        /// </summary>
        [JsonSchemaType(typeof(string), IsNullable = false)]
        public BigInteger Amount { get; set; }

        /// <summary>
        /// Internal TzKT id of the transaction operation, caused the token transfer.
        /// </summary>
        public long? TransactionId { get; set; }

        /// <summary>
        /// Internal TzKT id of the origination operation, caused the token transfer.
        /// </summary>
        public long? OriginationId { get; set; }

        /// <summary>
        /// Internal TzKT id of the migration operation, caused the token transfer.
        /// </summary>
        public long? MigrationId { get; set; }
    }
}
