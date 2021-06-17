using System.Text.Json.Serialization;

namespace Tzkt.Api.Services.Auth
{
    public class MetadataRecord
    {
        [JsonPropertyName("key")]
        public string Key { get; set; }
        
        [JsonPropertyName("metadata")]
        public RawJson Metadata { get; set; }
    }
}