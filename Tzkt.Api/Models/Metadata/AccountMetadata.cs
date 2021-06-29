using System.Text.Json.Serialization;

namespace Tzkt.Api.Models
{
    public class AccountMetadata
    {
        [JsonPropertyName("kind")]
        public string Kind { get; set; }

        [JsonPropertyName("alias")]
        public string Alias { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

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

        [JsonPropertyName("github")]
        public string Github { get; set; }

        [JsonPropertyName("gitlab")]
        public string Gitlab { get; set; }

        [JsonPropertyName("instagram")]
        public string Instagram { get; set; }

        [JsonPropertyName("facebook")]
        public string Facebook { get; set; }

        [JsonPropertyName("medium")]
        public string Medium { get; set; }
    }
}
