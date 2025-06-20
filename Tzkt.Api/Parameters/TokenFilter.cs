﻿using System.Text.Json.Serialization;
using Tzkt.Api.Services;

namespace Tzkt.Api
{
    public class TokenFilter : INormalizable
    {
        /// <summary>
        /// Filter by internal TzKT id. Note, this is not the same as `tokenId` nat value.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int64Parameter? id { get; set; }

        /// <summary>
        /// Filter by contract address.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public AccountParameter? contract { get; set; }

        /// <summary>
        /// Filter by tokenId (for FA1.2 tokens tokenId is always `"0"`).  
        /// Click on the parameter to expand more details.
        /// </summary>
        public NatParameter? tokenId { get; set; }

        /// <summary>
        /// Filter by "global id", which is a `"{contract}:{tokenId}"` string.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public TokenGlobalIdParameter? globalId { get; set; }

        /// <summary>
        /// Filter by token standard (`fa1.2` or `fa2`).  
        /// Click on the parameter to expand more details.
        /// </summary>
        public TokenStandardParameter? standard { get; set; }

        /// <summary>
        /// Filter by total minted.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public NatParameter? totalMinted { get; set; }

        /// <summary>
        /// Filter by total burned. Note: total burned shows only those tokens that were literally burned,
        /// excluding tokens sent to burn addresses (because they still exist on the burn address' balance).  
        /// Click on the parameter to expand more details.
        /// </summary>
        public NatParameter? totalBurned { get; set; }

        /// <summary>
        /// Filter by total supply. Note: total supply shows all existing tokens,
        /// including tokens sent to burn addresses (so that they exist on the burn address' balance).  
        /// Click on the parameter to expand more details.
        /// </summary>
        public NatParameter? totalSupply { get; set; }

        /// <summary>
        /// Filter by address of the first minter.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public AccountParameter? firstMinter { get; set; }

        /// <summary>
        /// Filter by level of the block where the token was first seen.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter? firstLevel { get; set; }

        /// <summary>
        /// Filter by timestamp (ISO 8601) of the block where the token was first seen.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public TimestampParameter? firstTime { get; set; }

        /// <summary>
        /// Filter by level of the block where the token was last seen.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter? lastLevel { get; set; }

        /// <summary>
        /// Filter by timestamp (ISO 8601) of the block where the token was last seen.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public TimestampParameter? lastTime { get; set; }

        /// <summary>
        /// Filter by metadata.  
        /// Note, this parameter supports the following format: `metadata{.path?}{.mode?}=...`,
        /// so you can specify a path to a particular field to filter by (for example, `?metadata.symbol.in=kUSD,uUSD`).  
        /// Click on the parameter to expand more details.
        /// </summary>
        public JsonParameter? metadata { get; set; }

        [JsonIgnore]
        public Int32NullParameter? indexedAt { get; set; }

        public string Normalize(string name)
        {
            return ResponseCacheService.BuildKey("",
                ("id", id), ("contract", contract), ("tokenId", tokenId), ("fullId", globalId), ("standard", standard), ("totalMinted", totalMinted),
                ("totalBurned", totalBurned), ("totalSupply", totalSupply), ("firstMinter", firstMinter), ("firstLevel", firstLevel),
                ("firstTime", firstTime), ("lastLevel", lastLevel), ("lastTime", lastTime), ("metadata", metadata), ("indexedAt", indexedAt));
        }
    }
}
