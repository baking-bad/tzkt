using System.Text.Json.Serialization;

namespace Tzkt.Api.Models
{
    public class BaseOperationError : OperationError
    {
        /// <summary>
        /// Type of an error
        /// https://tezos.gitlab.io/api/errors.html - full list of errors
        /// </summary>
        [JsonPropertyName("type")]
        public override string Type { get; set; }
    }
}
