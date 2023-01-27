using System.Text.Json.Serialization;

namespace Tzkt.Api.Services.Auth
{
    public class ExtrasUpdate<T> : ExtrasUpdate
    {
        [JsonPropertyName("key")]
        public T Key { get; set; }
    }

    public class ExtrasUpdate
    {
        [JsonPropertyName("extras")]
        public RawJson Extras { get; set; }

        #region deprecated
        [JsonPropertyName("metadata")]
        public RawJson Metadata => Extras;
        #endregion
    }
}