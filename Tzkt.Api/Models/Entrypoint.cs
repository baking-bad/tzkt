using System.Text.Json.Serialization;
using NJsonSchema.Annotations;
using Netezos.Encoding;

namespace Tzkt.Api.Models
{
    public class Entrypoint
    {
        /// <summary>
        /// Entrypoint name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// A kind of JSON schema, describing how parameters will look like in a human-readable JSON format
        /// </summary>
        [JsonSchemaType(typeof(object))]
        public RawJson JsonParameters { get; set; }

        /// <summary>
        /// Parameters schema in micheline format
        /// </summary>
        public IMicheline MichelineParameters { get; set; }

        /// <summary>
        /// Parameters schema in michelson format
        /// </summary>
        public string MichelsonParameters { get; set; }

        /// <summary>
        /// Unused means that the entrypoint can be normalized to a more specific one.
        /// For example here `(or %entry1 (unit %entry2) (nat %entry3))` the `%entry1` is unused entrypoint
        /// because it can be normalized to `%entry2` or `%entry3`
        /// </summary>
        public bool Unused { get; set; }
    }
}
