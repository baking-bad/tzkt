using System.Text.Json.Serialization;

namespace Tzkt.Api.Models
{
    public class Alias
    {
        /// <summary>
        /// Account alias name (offchain metadata).
        /// </summary>
        [JsonPropertyName("alias")]
        public string Name { get; set; }

        /// <summary>
        /// Account address (public key hash).
        /// </summary>
        [JsonPropertyName("address")]
        public string Address { get; set; }
    }
}
