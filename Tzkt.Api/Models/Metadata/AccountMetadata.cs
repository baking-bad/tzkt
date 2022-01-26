using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tzkt.Api.Models
{
    public class AccountMetadata
    {
        [JsonPropertyName("profile")]
        public ProfileMetadata Profile { get; set; }

        public static AccountMetadata Parse(string json)
        {
            try
            {
                if (json == null) return null;
                return JsonSerializer.Deserialize<AccountMetadata>(json);
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
