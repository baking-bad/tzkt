using System.Text.Json.Serialization;

namespace Tzkt.Sync.Protocols.Proto4
{
    class RawBakingRight
    {
        [JsonPropertyName("level")]
        public int Level { get; set; }

        [JsonPropertyName("priority")]
        public int Priority { get; set; }

        [JsonPropertyName("delegate")]
        public string Delegate { get; set; }

        #region validation
        public bool IsValidFormat() =>
            Level >= 0 &&
            Priority >= 0 &&
            !string.IsNullOrEmpty(Delegate);
        #endregion
    }
}
