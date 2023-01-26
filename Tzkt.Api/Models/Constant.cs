using System;
using NJsonSchema.Annotations;

namespace Tzkt.Api.Models
{
    public class Constant
    {
        /// <summary>
        /// Global address (expression hash)
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// Constant value (either micheline, michelson or bytes, depending on the `format` parameter)
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// Constant size in bytes
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        /// Number of contracts referencing this constant
        /// </summary>
        public int Refs { get; set; }

        /// <summary>
        /// Account registered this constant
        /// </summary>
        public Alias Creator { get; set; }

        /// <summary>
        /// Level of the first block baked with this software
        /// </summary>
        public int CreationLevel { get; set; }

        /// <summary>
        /// Datetime of the first block baked with this software
        /// </summary>
        public DateTime CreationTime { get; set; }

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
