using System.Collections.Generic;
using System.Text.Json.Serialization;
using Tzkt.Data.Models;

namespace Tzkt.Api.Models
{
    public class BigMap
    {
        /// <summary>
        /// Bigmap Id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Smart contract in which's storage the bigmap is allocated
        /// </summary>
        public Alias Contract { get; set; }

        /// <summary>
        /// Path in the JSON storage to the bigmap
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Bigmap status: `true` - active, `false` - removed
        /// </summary>
        public bool Active { get; set; }

        /// <summary>
        /// Level of the block where the bigmap was seen first time
        /// </summary>
        public int FirstLevel { get; set; }

        /// <summary>
        /// Level of the block where the bigmap was seen last time
        /// </summary>
        public int LastLevel { get; set; }

        /// <summary>
        /// Total number of keys ever added to the bigmap
        /// </summary>
        public int TotalKeys { get; set; }

        /// <summary>
        /// Total number of active (current) keys
        /// </summary>
        public int ActiveKeys { get; set; }

        /// <summary>
        /// Total number of actions with the bigmap
        /// </summary>
        public int Updates { get; set; }

        /// <summary>
        /// Bigmap key type as JSON schema or Micheline, depending on the `micheline` query parameter.
        /// </summary>
        public object KeyType { get; set; }

        /// <summary>
        /// Bigmap value type as JSON schema or Micheline, depending on the `micheline` query parameter.
        /// </summary>
        public object ValueType { get; set; }

        /// <summary>
        /// List of tags (`token_metadata` - tzip-12, `metadata` - tzip-16, `null` - no tags)
        /// </summary>
        public List<string> Tags => GetTagsList(_Tags);

        [JsonIgnore]
        public BigMapTag _Tags { get; set; }

        #region static
        public static List<string> GetTagsList(BigMapTag tags) => tags == BigMapTag.None ? null : new(1)
        {
            tags == BigMapTag.TokenMetadata ? BigMapTags.TokenMetadata : BigMapTags.Metadata
        };
        #endregion
    }
}
