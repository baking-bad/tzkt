using System.Text.Json.Serialization;

namespace Tzkt.Api.Services.Auth
{
    public class MetadataUpdate<T> : MetadataUpdate
    {
        [JsonPropertyName("key")]
        public T Key { get; set; }
    }

    public class MetadataUpdate
    {
        [JsonPropertyName("section")]
        public string Section { get; set; }

        [JsonPropertyName("metadata")]
        public RawJson Metadata { get; set; }
    }
}