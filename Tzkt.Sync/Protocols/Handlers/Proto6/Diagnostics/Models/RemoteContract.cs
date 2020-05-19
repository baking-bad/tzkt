using System.Text.Json.Serialization;

namespace Tzkt.Sync.Protocols.Proto6
{
    class RemoteContract
    {
        [JsonPropertyName("balance")]
        public long Balance { get; set; }

        [JsonPropertyName("delegate")]
        public string Delegate { get; set; }

        [JsonPropertyName("counter")]
        public long? Counter { get; set; }

        #region validation
        public bool IsValidFormat() =>
            Balance != null;
        #endregion
    }
}
