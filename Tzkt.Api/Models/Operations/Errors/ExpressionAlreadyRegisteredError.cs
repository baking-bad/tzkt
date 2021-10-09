using System.Text.Json.Serialization;

namespace Tzkt.Api.Models
{
    public class ExpressionAlreadyRegisteredError : OperationError
    {
        /// <summary>
        /// Type of an error, `Expression_already_registered` - an operation tried to register
        /// an already existing global consant
        /// https://tezos.gitlab.io/api/errors.html - full list of errors
        /// </summary>
        [JsonPropertyName("type")]
        public override string Type { get; set; }

        /// <summary>
        /// Global address of the constant
        /// </summary>
        [JsonPropertyName("expression")]
        public string Expression { get; set; }
    }
}
