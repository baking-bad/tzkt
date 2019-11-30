using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class NonExistingContractError : OperationError
    {
        [JsonPropertyName("type")]
        public override string Type { get; set; }

        [JsonPropertyName("contract")]
        public string Contract { get; set; }
    }
}
