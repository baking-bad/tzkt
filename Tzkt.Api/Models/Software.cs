using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using NJsonSchema.Annotations;

namespace Tzkt.Api.Models
{
    public class Software
    {
        /// <summary>
        /// Software ID (short commit hash)
        /// </summary>
        public string ShortHash { get; set; }

        /// <summary>
        /// Level of the first block baked with this software
        /// </summary>
        public int FirstLevel { get; set; }

        /// <summary>
        /// Datetime of the first block baked with this software
        /// </summary>
        public DateTime FirstTime { get; set; }

        /// <summary>
        /// Level of the last block baked with this software
        /// </summary>
        public int LastLevel { get; set; }

        /// <summary>
        /// Datetime of the last block baked with this software
        /// </summary>
        public DateTime LastTime { get; set; }

        /// <summary>
        /// Total number of blocks baked with this software
        /// </summary>
        public int BlocksCount { get; set; }

        /// <summary>
        /// Off-chain extras
        /// </summary>
        [JsonSchemaType(typeof(object), IsNullable = true)]
        public RawJson Extras { get; set; }

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public RawJson Metadata => Extras;
    }
}
