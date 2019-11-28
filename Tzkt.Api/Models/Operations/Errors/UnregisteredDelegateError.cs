using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class UnregisteredDelegateError : IOperationError
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("delegate")]
        public string Delegate { get; set; }
    }
}
