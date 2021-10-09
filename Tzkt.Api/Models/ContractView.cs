using NJsonSchema.Annotations;
using Netezos.Encoding;

namespace Tzkt.Api.Models
{
    public class ContractView
    {
        /// <summary>
        /// Contract view name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Parameter type in human-readable JSON format
        /// </summary>
        [JsonSchemaType(typeof(object))]
        public RawJson JsonParameterType { get; set; }

        /// <summary>
        /// Return type in human-readable JSON format
        /// </summary>
        [JsonSchemaType(typeof(object))]
        public RawJson JsonReturnType { get; set; }

        /// <summary>
        /// Parameter type in micheline format
        /// </summary>
        public IMicheline MichelineParameterType { get; set; }

        /// <summary>
        /// Return type in micheline format
        /// </summary>
        public IMicheline MichelineReturnType { get; set; }

        /// <summary>
        /// Parameter type in michelson format
        /// </summary>
        public string MichelsonParameterType { get; set; }

        /// <summary>
        /// Return type in michelson format
        /// </summary>
        public string MichelsonReturnType { get; set; }
    }
}
