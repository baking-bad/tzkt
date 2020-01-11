using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace Tzkt.Api.Services.Metadata
{
    public class AccountMetadata
    {
        [JsonPropertyName("address")]
        public string Address { get; set; }

        [JsonPropertyName("kind")]
        public string Kind { get; set; }

        [JsonPropertyName("owner")]
        public string Owner { get; set; }

        [JsonPropertyName("alias")]
        public string Alias { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("logo")]
        public string Logo { get; set; }

        [JsonPropertyName("site")]
        public string Site { get; set; }

        [JsonPropertyName("support")]
        public string Support { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("twitter")]
        public string Twitter { get; set; }

        [JsonPropertyName("telegram")]
        public string Telegram { get; set; }

        [JsonPropertyName("discord")]
        public string Discord { get; set; }

        [JsonPropertyName("reddit")]
        public string Reddit { get; set; }

        [JsonPropertyName("slack")]
        public string Slack { get; set; }

        [JsonPropertyName("riot")]
        public string Riot { get; set; }

        [JsonPropertyName("github")]
        public string Github { get; set; }

        public override string ToString() => Alias ?? Address;
    }
}
