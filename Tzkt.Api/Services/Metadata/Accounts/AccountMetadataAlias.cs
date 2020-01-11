using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace Tzkt.Api.Services.Metadata
{
    public class AccountMetadataAlias
    {
        [JsonPropertyName("address")]
        public string Address { get; set; }

        [JsonPropertyName("alias")]
        public string Alias { get; set; }

        [JsonPropertyName("logo")]
        public string Logo { get; set; }
    }
}
