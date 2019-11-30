using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class BalanceTooLowError : OperationError
    {
        [JsonPropertyName("type")]
        public override string Type { get; set; }

        [JsonPropertyName("balance")]
        public long Balance { get; set; }

        [JsonPropertyName("required")]
        public long Required { get; set; }
    }
}
