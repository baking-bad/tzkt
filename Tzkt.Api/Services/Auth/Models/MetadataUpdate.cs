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
        [JsonPropertyName("metadata")]
        public RawJson Metadata { get; set; }
    }
}