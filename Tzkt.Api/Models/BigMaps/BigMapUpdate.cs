using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Tzkt.Data.Models;

namespace Tzkt.Api.Models
{
    public class BigMapUpdate
    {
        /// <summary>
        /// Internal Id, can be used for pagination
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Level of the block where the bigmap was updated
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// Timestamp of the block where the bigmap was updated
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Bigmap ptr
        /// </summary>
        public int Bigmap { get; set; }

        /// <summary>
        /// Smart contract in which's storage the bigmap is allocated
        /// </summary>
        public Alias Contract { get; set; }

        /// <summary>
        /// Path to the bigmap in the contract storage
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Action with the bigmap (`allocate`, `add_key`, `update_key`, `remove_key`, `remove`)
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        /// Updated key.
        /// If the action is `allocate` or `remove` the content will be `null`.
        /// </summary>
        public BigMapKeyShort Content { get; set; }

        [JsonIgnore]
        public BigMapTag _Tags { get; set; }

        public IEnumerable<BigMapTag> EnumerateTags()
        {
            if (_Tags >= BigMapTag.Metadata)
            {
                if ((_Tags & BigMapTag.Metadata) != 0)
                    yield return BigMapTag.Metadata;
                else if ((_Tags & BigMapTag.TokenMetadata) != 0)
                    yield return BigMapTag.TokenMetadata;
                else if ((_Tags & BigMapTag.Ledger) != 0)
                    yield return BigMapTag.Ledger;
            }
        }
    }
}
