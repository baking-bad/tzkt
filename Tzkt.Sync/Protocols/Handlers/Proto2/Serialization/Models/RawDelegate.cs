using System.Text.Json.Serialization;

namespace Tzkt.Sync.Protocols.Proto2
{
    class RawDelegate
    {
        [JsonPropertyName("deactivated")]
        public bool Deactivated { get; set; }

        [JsonPropertyName("grace_period")]
        public int GracePeriod { get; set; }

        #region validation
        public bool IsValidFormat() => GracePeriod >= 0;
        #endregion
    }
}
