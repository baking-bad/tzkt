using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class Alias
    {
        /// <summary>
        /// Name of the project behind the account or account description
        /// </summary>
        [JsonPropertyName("alias")]
        public string Name { get; set; }

        /// <summary>
        /// Public key hash of the account
        /// </summary>
        [JsonPropertyName("address")]
        public string Address { get; set; }
    }
}
