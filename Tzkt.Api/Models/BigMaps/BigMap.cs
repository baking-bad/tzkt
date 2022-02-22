using System.Collections.Generic;
using System.Text.Json.Serialization;
using Tzkt.Data.Models;

namespace Tzkt.Api.Models
{
    public class BigMap
    {
        /// <summary>
        /// Bigmap pointer
        /// </summary>
        public int Ptr { get; set; }

        /// <summary>
        /// Smart contract in which's storage the bigmap is allocated
        /// </summary>
        public Alias Contract { get; set; }

        /// <summary>
        /// Path to the bigmap in the contract storage 
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// List of tags ( `metadata`, `token_metadata`,`ledger`, or `null` if there are no tags)
        /// </summary>
        public List<string> Tags => BigMapTags.ToList(_Tags);

        /// <summary>
        /// Bigmap status (`true` - active, `false` - removed)
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
        /// Total number of currently active keys
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

        [JsonIgnore]
        public BigMapTag _Tags { get; set; }
    }
}
