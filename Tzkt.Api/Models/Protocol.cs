using NJsonSchema.Annotations;

namespace Tzkt.Api.Models
{
    public class Protocol
    {
        /// <summary>
        /// Protocol code, representing a number of protocol changes since genesis (mod 256, but `-1` for the genesis block)
        /// </summary>
        public int Code { get; set; }

        /// <summary>
        /// Hash of the protocol
        /// </summary>
        public string Hash { get; set; }

        /// <summary>
        /// Block height where the protocol was applied
        /// </summary>
        public int  FirstLevel { get; set; }

        /// <summary>
        /// Block height where the protocol ends. `null` if the protocol is active
        /// </summary>
        public int? LastLevel { get; set; }
        
        /// <summary>
        /// Information about the protocol constants
        /// </summary>
        public ProtocolConstants Constants { get; set; }

        /// <summary>
        /// Offchain metadata
        /// </summary>
        [JsonSchemaType(typeof(ProtocolMetadata), IsNullable = true)]
        public RawJson Metadata { get; set; }
    }
}
