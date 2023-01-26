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
        /// Index of the first cycle started with the protocol
        /// </summary>
        public int FirstCycle { get; set; }

        /// <summary>
        /// Level of the first block of the first cycle started with the protocol
        /// </summary>
        public int FirstCycleLevel { get; set; }

        /// <summary>
        /// Block height where the protocol ends. `null` if the protocol is active
        /// </summary>
        public int? LastLevel { get; set; }
        
        /// <summary>
        /// Information about the protocol constants
        /// </summary>
        public ProtocolConstants Constants { get; set; }

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
