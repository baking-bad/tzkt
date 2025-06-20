﻿using System.Numerics;
using NJsonSchema.Annotations;

namespace Tzkt.Api.Models
{
    public class Token
    {
        /// <summary>
        /// Internal TzKT id (not the same as `tokenId`).  
        /// **[sortable]**
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Contract, created the token.
        /// </summary>
        public required Alias Contract { get; set; }

        /// <summary>
        /// Token id, unique within the contract.  
        /// **[sortable]**
        /// </summary>
        [JsonSchemaType(typeof(string), IsNullable = false)]
        public BigInteger TokenId { get; set; }

        /// <summary>
        /// Token standard (`fa1.2` or `fa2`).
        /// </summary>
        public required string Standard { get; set; }

        /// <summary>
        /// Account, minted the token first.
        /// </summary>
        public required Alias FirstMinter { get; set; }

        /// <summary>
        /// Level of the block where the token was first seen.  
        /// **[sortable]**
        /// </summary>
        public int FirstLevel { get; set; }

        /// <summary>
        /// Timestamp of the block where the token was first seen.
        /// </summary>
        public DateTime FirstTime { get; set; }

        /// <summary>
        /// Level of the block where the token was last seen.  
        /// **[sortable]**
        /// </summary>
        public int LastLevel { get; set; }

        /// <summary>
        /// Timestamp of the block where the token was last seen.
        /// </summary>
        public DateTime LastTime { get; set; }

        /// <summary>
        /// Total number of transfers.  
        /// **[sortable]**
        /// </summary>
        public int TransfersCount { get; set; }

        /// <summary>
        /// Total number of holders ever seen.  
        /// **[sortable]**
        /// </summary>
        public int BalancesCount { get; set; }

        /// <summary>
        /// Total number of current holders.  
        /// **[sortable]**
        /// </summary>
        public int HoldersCount { get; set; }

        /// <summary>
        /// Total number of minted tokens (raw value, not divided by `decimals`).
        /// </summary>
        [JsonSchemaType(typeof(string), IsNullable = false)]
        public BigInteger TotalMinted { get; set; }

        /// <summary>
        /// Total number of burned tokens (raw value, not divided by `decimals`).
        /// </summary>
        [JsonSchemaType(typeof(string), IsNullable = false)]
        public BigInteger TotalBurned { get; set; }

        /// <summary>
        /// Total number of existing tokens (raw value, not divided by `decimals`).
        /// </summary>
        [JsonSchemaType(typeof(string), IsNullable = false)]
        public BigInteger TotalSupply { get; set; }

        /// <summary>
        /// Token metadata.  
        /// **[sortable]**
        /// </summary>
        [JsonSchemaType(typeof(object), IsNullable = true)]
        public RawJson? Metadata { get; set; }
    }
}
